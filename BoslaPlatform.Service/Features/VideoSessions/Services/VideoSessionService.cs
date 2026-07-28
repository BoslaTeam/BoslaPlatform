using AutoMapper;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Observability;
using BoslaPlatform.Application.Interfaces.Storage;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Application.Features.VideoSessions.Services
{
    public class VideoSessionService : IVideoSessionService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IAgoraTokenService _agoraTokenService;
        private readonly IRecordingProvider _recordingProvider;
        private readonly IVideoSessionLifecycleService _lifecycleService;
        private readonly IRecordingCompletionService _completionService;
        private readonly IMapper _mapper;
        private readonly ILogger<VideoSessionService> _logger;
        private readonly IRecordingPipelineLog _pipelineLog;

        public VideoSessionService(
            IAppDbContext context,
            IUser currentUser,
            IAgoraTokenService agoraTokenService,
            IRecordingProvider recordingProvider,
            IVideoSessionLifecycleService lifecycleService,
            IRecordingCompletionService completionService,
            IMapper mapper,
            ILogger<VideoSessionService> logger,
            IRecordingPipelineLog pipelineLog)
        {
            _context = context;
            _currentUser = currentUser;
            _agoraTokenService = agoraTokenService;
            _recordingProvider = recordingProvider;
            _lifecycleService = lifecycleService;
            _completionService = completionService;
            _mapper = mapper;
            _logger = logger;
            _pipelineLog = pipelineLog;
        }

        private Guid CurrentUserId => _currentUser.Id!.Value;

        private Result EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
                return Error.Unauthorized("User.Unauthorized", "User is not authenticated.");
            return Result.Success();
        }

        private async Task<Specialist?> GetCurrentSpecialistAsync(CancellationToken ct)
        {
            return await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == CurrentUserId, ct);
        }

        private async Task<Result<VideoSession>> LoadSessionAsync(
            Guid sessionId,
            CancellationToken ct,
            params Func<IQueryable<VideoSession>, IQueryable<VideoSession>>[] includes)
        {
            IQueryable<VideoSession> query = _context.VideoSessions;

            foreach (var include in includes)
                query = include(query);

            if (includes.Length > 1)
                query = query.AsSplitQuery();

            var session = await query.FirstOrDefaultAsync(x => x.Id == sessionId, ct);

            if (session is null)
                return Error.NotFound("VideoSession.NotFound", "Video session was not found.");

            return Result<VideoSession>.Success(session);
        }

        private async Task<Result<VideoSession>> LoadSessionReadOnlyAsync(
            Guid sessionId,
            CancellationToken ct,
            params Func<IQueryable<VideoSession>, IQueryable<VideoSession>>[] includes)
        {
            IQueryable<VideoSession> query = _context.VideoSessions.AsNoTracking();

            foreach (var include in includes)
                query = include(query);

            if (includes.Length > 1)
                query = query.AsSplitQuery();

            var session = await query.FirstOrDefaultAsync(x => x.Id == sessionId, ct);

            if (session is null)
                return Error.NotFound("VideoSession.NotFound", "Video session was not found.");

            return Result<VideoSession>.Success(session);
        }

        private async Task<Result> ValidateAssignedSpecialistAsync(
            VideoSession session,
            CancellationToken ct,
            string? action = null)
        {
            var specialist = await GetCurrentSpecialistAsync(ct);

            var isAssigned = specialist is not null
                && session.Appointment!.SpecialistId == specialist.Id;

            if (!isAssigned)
                return Error.Forbidden("VideoSession.AccessDenied",
                    action is not null
                        ? $"Only the assigned specialist can {action}."
                        : "Only the assigned specialist can perform this action.");

            return Result.Success();
        }

        private async Task<Result> ValidateAppointmentMembershipAsync(Appointment appointment, CancellationToken ct)
        {
            var isClient = appointment.UserId == CurrentUserId;

            var specialist = await GetCurrentSpecialistAsync(ct);

            var isSpecialist = specialist is not null && appointment.SpecialistId == specialist.Id;

            if (!isClient && !isSpecialist)
                return Error.Forbidden("VideoSession.AccessDenied", "You are not authorized to access this video session.");

            return Result.Success();
        }

        public async Task<Result<VideoSessionDto>> GetByIdAsync(
            Guid sessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionReadOnlyAsync(sessionId, ct,
                q => q.Include(x => x.Participants).ThenInclude(p => p.User),
                q => q.Include(x => x.Appointment),
                q => q.Include(x => x.CurrentRecording));

            if (sessionResult.IsError) return sessionResult.Errors;

            var membership = await ValidateAppointmentMembershipAsync(sessionResult.Value.Appointment!, ct);
            if (membership.IsError) return membership.Errors;

            var dto = _mapper.Map<VideoSessionDto>(sessionResult.Value);
            return Result<VideoSessionDto>.Success(dto);
        }

        public async Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default)
        {
            return await _agoraTokenService.GenerateTokenAsync(appointmentId, ct);
        }

        public async Task<Result<JoinVideoSessionResponse>> JoinAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment),
                q => q.Include(x => x.Participants));

            if (sessionResult.IsError) return sessionResult.Errors;

            var membership = await ValidateAppointmentMembershipAsync(sessionResult.Value.Appointment!, ct);
            if (membership.IsError) return membership.Errors;

            var session = sessionResult.Value;
            var appointment = session.Appointment!;

            if (session.Status is VideoSessionStatus.Completed or VideoSessionStatus.Ended)
                return Error.Validation("VideoSession.Ended", "This video session has ended and cannot be joined.");

            if (appointment.Status != AppointmentStatus.Paid)
                return Error.Validation("Appointment.NotPaid", "The appointment must be paid before joining the video session.");

            var timeValidation = appointment.CanJoinVideoSession(DateTimeOffset.UtcNow);
            if (timeValidation.IsError) return timeValidation.Errors;

            var participant = session.Participants.FirstOrDefault(x => x.UserId == CurrentUserId);

            if (participant is null)
                return Error.Validation("VideoSession.InvalidState", "You must generate a token before joining the session.");

            if (participant.LeftAt is not null)
            {
                var rejoinResult = session.ParticipantRejoined(CurrentUserId);
                if (rejoinResult.IsError) return rejoinResult.Errors;

                await _context.SaveChangesAsync(ct);
            }

            return Result<JoinVideoSessionResponse>.Success(
                new JoinVideoSessionResponse(session.Id, CurrentUserId, DateTime.UtcNow));
        }

        public async Task<Result<LeaveVideoSessionResponse>> LeaveAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                q => q.Include(x => x.Participants));

            if (sessionResult.IsError) return sessionResult.Errors;

            var session = sessionResult.Value;
            var participant = session.Participants.FirstOrDefault(x => x.UserId == CurrentUserId);

            if (participant is null)
                return Error.Validation("VideoSessionParticipant.NotFound", "You are not a participant of this session.");

            if (participant.LeftAt is not null)
                return Error.Validation("VideoSessionParticipant.AlreadyLeft", "You have already left this session.");

            var result = session.MarkParticipantLeft(CurrentUserId);
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);

            return Result<LeaveVideoSessionResponse>.Success(
                new LeaveVideoSessionResponse(session.Id, CurrentUserId, DateTime.UtcNow));
        }

        public async Task<Result<FinishConsultationResponse>> FinishConsultationAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment));

            if (sessionResult.IsError) return sessionResult.Errors;

            var specialistCheck = await ValidateAssignedSpecialistAsync(sessionResult.Value, ct, "finish this consultation");
            if (specialistCheck.IsError) return specialistCheck.Errors;

            var lifecycleResult = await _lifecycleService.CompleteSessionAsync(
                videoSessionId, VideoSessionCompletionReason.SpecialistEnded, ct);

            if (lifecycleResult.IsError) return lifecycleResult.Errors;

            return Result<FinishConsultationResponse>.Success(
                new FinishConsultationResponse(videoSessionId, sessionResult.Value.EndedAt!.Value));
        }

        public async Task<Result<StartVideoSessionResponse>> StartAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment));

            if (sessionResult.IsError) return sessionResult.Errors;

            var specialistCheck = await ValidateAssignedSpecialistAsync(sessionResult.Value, ct, "start this session");
            if (specialistCheck.IsError) return specialistCheck.Errors;

            var appointment = sessionResult.Value.Appointment!;

            //if (appointment.Status != AppointmentStatus.Paid)
            //    return Error.Validation("Appointment.NotPaid", "The appointment must be paid before starting the video session.");

            var result = sessionResult.Value.Start();
            if (result.IsError) return result.Errors;

            await _context.SaveChangesAsync(ct);

            return Result<StartVideoSessionResponse>.Success(
                new StartVideoSessionResponse(sessionResult.Value.Id, sessionResult.Value.StartedAt!.Value));
        }

        public async Task<Result<EndVideoSessionResponse>> EndAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment));

            if (sessionResult.IsError) return sessionResult.Errors;

            var specialistCheck = await ValidateAssignedSpecialistAsync(sessionResult.Value, ct, "end this session");
            if (specialistCheck.IsError) return specialistCheck.Errors;

            // "End Call" is a specialist action that ends the consultation. It must go
            // through the single completion path (identical to Finish Consultation) so
            // the session reaches Completed and recording/STT cleanup runs — rather than
            // leaving it stuck in Ended with an orphaned Agora recording.
            var lifecycleResult = await _lifecycleService.CompleteSessionAsync(
                videoSessionId, VideoSessionCompletionReason.SpecialistEnded, ct);

            if (lifecycleResult.IsError) return lifecycleResult.Errors;

            return Result<EndVideoSessionResponse>.Success(
                new EndVideoSessionResponse(sessionResult.Value.Id, sessionResult.Value.EndedAt!.Value));
        }

        public async Task<Result<StartRecordingResponse>> StartRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            _logger.LogInformation("Recording requested for session {SessionId}", videoSessionId);

            // --------------------------------------------------------------
            // Phase 1: Domain + persistence (short SQL transaction)
            // --------------------------------------------------------------
            ScreenRecording recording = null!;
            VideoSession session = null!;

            await using (var tx1 = await _context.BeginTransactionAsync(ct))
            {
                var sessionResult = await LoadSessionAsync(videoSessionId, ct,
                    q => q.Include(x => x.Appointment),
                    q => q.Include(x => x.CurrentRecording),
                    q => q.Include(x => x.Recordings));

                if (sessionResult.IsError) return sessionResult.Errors;
                session = sessionResult.Value;

                var specialistCheck = await ValidateAssignedSpecialistAsync(session, ct, "start recording");
                if (specialistCheck.IsError) return specialistCheck.Errors;

                var recordingResult = ScreenRecording.Create(
                    session.Id,
                    RecordingAccessControl.Both,
                    RecordingStorageProvider.Agora);

                if (recordingResult.IsError)
                    return recordingResult.Errors;

                recording = recordingResult.Value;

                var startResult = session.StartRecording(recording);

                if (startResult.IsError)
                    return startResult.Errors;

                _context.ScreenRecordings.Add(recording);

                await _context.SaveChangesAsync(ct);

                var setCurrentResult = session.SetCurrentRecording(recording);

                if (setCurrentResult.IsError)
                    return setCurrentResult.Errors;

                await _context.SaveChangesAsync(ct);

                await tx1.CommitAsync(ct);
            }

            // --------------------------------------------------------------
            // Phase 2: External provider call (NO transaction)
            // --------------------------------------------------------------
            _logger.LogInformation("Provider recording start requested for session {SessionId}, channel {Channel}",
                videoSessionId, session.AgoraChannelName);

            var providerResult = await _recordingProvider.StartRecordingAsync(
                session.AgoraChannelName, ct);

            if (providerResult.IsError)
            {
                _logger.LogWarning("Provider recording start failed for session {SessionId}, channel {Channel}",
                    videoSessionId, session.AgoraChannelName);

                // ----------------------------------------------------------
                // Compensation: short TX to restore aggregate consistency
                // ----------------------------------------------------------
                await using (var compensateTx = await _context.BeginTransactionAsync(ct))
                {
                    session.FailActiveRecording("Provider recording start failed after Acquire.");

                    await _context.SaveChangesAsync(ct);
                    await compensateTx.CommitAsync(ct);
                }

                return providerResult.Errors;
            }



            // --------------------------------------------------------------
            // Phase 3: Persist provider identifiers (short SQL transaction)
            // --------------------------------------------------------------
            var providerData = providerResult.Value;
            _logger.LogInformation(
                "Provider recording started successfully for session {SessionId}. ResourceId={ResourceId}, SID={Sid}, RecordingUid={RecordingUid}",
                videoSessionId,
                providerData.ProviderRecordingId,
                providerData.ProviderRecordingSid,
                providerData.RecordingUid);

            await using (var tx2 = await _context.BeginTransactionAsync(ct))
            {
                recording.AttachAgoraRecording(
                    providerData.ProviderRecordingId,
                    providerData.ProviderRecordingSid,
                    providerData.RecordingUid);

                session.SetRecording(
                    providerData.ProviderRecordingId,
                    providerData.ProviderRecordingSid,
                    providerData.RecordingUid);

                await _context.SaveChangesAsync(ct);
                await tx2.CommitAsync(ct);
            }

            _logger.LogInformation("Recording persistence completed for session {SessionId}",
                videoSessionId);

            return Result<StartRecordingResponse>.Success(
                new StartRecordingResponse(
                    session.Id,
                    recording.Id,
                    session.RecordingStartedAtUtc!.Value));
        }

        public async Task<Result<StopRecordingResponse>> StopRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            _logger.LogInformation("Recording stop requested for session {SessionId}", videoSessionId);

            // --------------------------------------------------------------
            // Authorize the caller (assigned specialist) and confirm a recording is
            // running. The Stop + upload verification + persistence all happen inside
            // the shared IRecordingCompletionService — the single completion path.
            // --------------------------------------------------------------
            var sessionResult = await LoadSessionReadOnlyAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment));

            if (sessionResult.IsError) return sessionResult.Errors;

            var specialistCheck = await ValidateAssignedSpecialistAsync(sessionResult.Value, ct, "stop recording");
            if (specialistCheck.IsError) return specialistCheck.Errors;

            if (!sessionResult.Value.IsRecording)
            {
                return Error.Validation(
                    "VideoSession.NotRecording",
                    "No active recording to stop.");
            }

            // --------------------------------------------------------------
            // Single completion pipeline: Stop → verify upload → persist state.
            // --------------------------------------------------------------
            var completion = await _completionService.CompleteAsync(
                videoSessionId, RecordingCompletionTrigger.ManualStop, hint: null, ct);

            if (completion.IsError)
            {
                _logger.LogWarning(
                    "Manual stop completion failed for session {SessionId}: {Error}",
                    videoSessionId, completion.Errors[0].Description);
                return completion.Errors;
            }

            var finalStatus = completion.Value.FinalStatus;

            return Result<StopRecordingResponse>.Success(
                new StopRecordingResponse(
                    videoSessionId,
                    new RecordingInfoDto
                    {
                        Status = finalStatus,
                        IsRecording = false,
                        CanStartRecording = false,
                        CanStopRecording = false
                    }));
        }

        public async Task<Result<RecordingInfoDto>> GetRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            var auth = EnsureAuthenticated();
            if (auth.IsError) return auth.Errors;

            var sessionResult = await LoadSessionReadOnlyAsync(videoSessionId, ct,
                q => q.Include(x => x.Appointment),
                q => q.Include(x => x.CurrentRecording));

            if (sessionResult.IsError) return sessionResult.Errors;

            var membership = await ValidateAppointmentMembershipAsync(sessionResult.Value.Appointment!, ct);
            if (membership.IsError) return membership.Errors;

            var session = sessionResult.Value;
            var appointment = session.Appointment!;

            var specialist = await GetCurrentSpecialistAsync(ct);

            var isSpecialist = specialist is not null
                && appointment.SpecialistId == specialist.Id;

            var dto = _mapper.Map<RecordingInfoDto>(session.CurrentRecording)
                ?? new RecordingInfoDto();

            dto.Status = session.RecordingStatus;
            dto.StartedAtUtc = session.RecordingStartedAtUtc;
            dto.CompletedAtUtc = session.RecordingCompletedAt;
            dto.IsRecording = session.IsRecording;
            dto.CurrentRecordingId = session.CurrentRecordingId;
            dto.Url ??= session.RecordingUrl;
            dto.RecordingFailureReason = session.RecordingFailureReason;

            dto.CanStartRecording = isSpecialist
                && session.Status == VideoSessionStatus.Active
                && session.RecordingStatus is null;

            dto.CanStopRecording = isSpecialist
                && session.IsRecording;

            return Result<RecordingInfoDto>.Success(dto);
        }
    }
}
