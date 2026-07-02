using AutoMapper;
using BoslaPlatform.Application.Features.VideoSessions.Dtos;
using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Application.Features.VideoSessions.Services
{
    public class VideoSessionService : IVideoSessionService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IAgoraTokenService _agoraTokenService;
        private readonly IRecordingProvider _recordingProvider;
        private readonly IMapper _mapper;

        public VideoSessionService(
            IAppDbContext context,
            IUser currentUser,
            IAgoraTokenService agoraTokenService,
            IRecordingProvider recordingProvider,
            IMapper mapper)
        {
            _context = context;
            _currentUser = currentUser;
            _agoraTokenService = agoraTokenService;
            _recordingProvider = recordingProvider;
            _mapper = mapper;
        }

        /// <summary>
        /// Retrieves a video session by its unique identifier.
        /// Validates authentication and appointment membership before returning session details.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the video session.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A Result containing the VideoSessionDto or an appropriate error.</returns>
        public async Task<Result<VideoSessionDto>> GetByIdAsync(
            Guid sessionId,
            CancellationToken ct = default)
        {
            // 1. Validate authentication
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            // 2. Retrieve session with participants and their user information
            var session = await _context.VideoSessions
                .AsNoTracking()
                .Include(x => x.Participants)
                    .ThenInclude(p => p.User)
                .Include(x => x.Appointment)
                .Include(x => x.CurrentRecording)
                .FirstOrDefaultAsync(
                    x => x.Id == sessionId,
                    ct);

            // 3. Validate session exists
            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }

            // 4. Validate current user belongs to the appointment
            var appointment = session.Appointment!;
            var isClient = appointment.UserId == _currentUser.Id;

            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.UserId == _currentUser.Id,
                    ct);

            var isSpecialist =
                specialist is not null &&
                appointment.SpecialistId == specialist.Id;

            if (!isClient && !isSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "You are not authorized to access this video session.");
            }

            // 5. Map and return
            var dto = _mapper.Map<VideoSessionDto>(session);

            return Result<VideoSessionDto>.Success(dto);
        }

        /// <summary>
        /// Generates an Agora RTC token for a video session associated with an appointment.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A Result containing the AgoraTokenResponse or an appropriate error.</returns>
        public async Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default)
        {
            var tokenResult = await _agoraTokenService.GenerateTokenAsync(
                appointmentId,
                ct);

            return tokenResult;
        }
        //public async Task<Result<JoinVideoSessionResponse>> JoinAsync( Guid videoSessionId,CancellationToken ct = default)
        //{
        //    if (!_currentUser.IsAuthenticated ||
        //        _currentUser.Id is null)
        //    {
        //        return Error.Unauthorized(
        //            "User.Unauthorized",
        //            "User is not authenticated.");
        //    }

        //    var session = await _context.VideoSessions
        //        .Include(x => x.Participants)
        //        .FirstOrDefaultAsync(
        //            x => x.Id == videoSessionId,
        //            ct);

        //    if (session is null)
        //    {
        //        return Error.NotFound(
        //            "VideoSession.NotFound",
        //            "Video session was not found.");
        //    }

        //    var uid = GenerateAgoraUid(
        //        _currentUser.Id.Value);

        //    var role = VideoParticipantRole.Participant;

        //    var result = session.AddParticipant(
        //        _currentUser.Id.Value,
        //        uid,
        //        role);

        //    if (result.IsError)
        //    {
        //        return result.Errors;
        //    }

        //    await _context.SaveChangesAsync(ct);

        //    return Result<JoinVideoSessionResponse>.Success(
        //        new JoinVideoSessionResponse(
        //            session.Id,
        //            _currentUser.Id.Value,
        //            DateTime.UtcNow));
        //}
        /// <summary>
        /// Prepares a video session for joining.
        ///
        /// This is a VALIDATION and PREPARATION step only.
        /// It does NOT activate the session — the session transitions to Active
        /// exclusively when Agora fires the channel_created webhook callback
        /// (VideoSession.ChannelCreated()). The specialist and participants can
        /// obtain tokens and prepare to join after this step succeeds.
        /// </summary>
        /// <param name="videoSessionId">
        /// Video session identifier.
        /// </param>
        /// <param name="ct">
        /// Cancellation token.
        /// </param>
        /// <returns>
        /// Preparation confirmation with acknowledgment timestamp.
        /// The actual StartedAt is set when Agora confirms the first participant join.
        /// </returns>
        public async Task<Result<StartVideoSessionResponse>> StartAsync(Guid videoSessionId,CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            var session = await _context.VideoSessions
                .Include(x => x.Appointment)
                .FirstOrDefaultAsync(
                    x => x.Id == videoSessionId,
                    ct);

            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }
            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(
                s => s.UserId == _currentUser.Id,
                ct);

            var isAssignedSpecialist =
                specialist is not null &&
                session.Appointment!.SpecialistId == specialist.Id;

            if (!isAssignedSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "Only the assigned specialist can start this session.");
            }

            //var validation = session.Appointment!.CanStartVideoSession(DateTimeOffset.UtcNow);

            //if (validation.IsError)
            //{
            //    return validation.Errors;
            //}

            var result = session.Start();

            if (result.IsError)
            {
                return result.Errors;
            }

            await _context.SaveChangesAsync(ct);

            return Result<StartVideoSessionResponse>.Success(
                new StartVideoSessionResponse(
                    session.Id,
                    session.StartedAt!.Value));
        }
        /// <summary>
        /// Ends a video session manually.
        /// The specialist can end the session before the scheduled appointment end.
        /// If the session has not been activated yet (Waiting status), ending it
        /// cancels the preparation — the session will be marked as Ended and
        /// will not transition to Active when Agora fires channel_created.
        /// </summary>
        /// <param name="videoSessionId">
        /// Video session identifier.
        /// </param>
        /// <param name="ct">
        /// Cancellation token.
        /// </param>
        /// <returns>
        /// Ended session details.
        /// </returns>
        public async Task<Result<EndVideoSessionResponse>> EndAsync( Guid videoSessionId,CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }
            var session = await _context.VideoSessions
                .Include(x => x.Appointment)
                .FirstOrDefaultAsync(
                    x => x.Id == videoSessionId,
                    ct);

            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }

            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(
                s => s.UserId == _currentUser.Id,
                ct);

            var isAssignedSpecialist =
                specialist is not null &&
                session.Appointment!.SpecialistId == specialist.Id;

            if (!isAssignedSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "Only the assigned specialist can end this session.");
            }

            var result = session.End();

            if (result.IsError)
            {
                return result.Errors;
            }

            await _context.SaveChangesAsync(ct);

            return Result<EndVideoSessionResponse>.Success(
                new EndVideoSessionResponse(
                    session.Id,
                    session.EndedAt!.Value));
        }

        public async Task<Result<StartRecordingResponse>> StartRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            await using var transaction = await _context.BeginTransactionAsync(ct);

            var session = await _context.VideoSessions
                .Include(x => x.Appointment)
                .Include(x => x.CurrentRecording)
                .Include(x => x.Recordings)
                .FirstOrDefaultAsync(x => x.Id == videoSessionId, ct);

            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }

            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.Id, ct);

            var isAssignedSpecialist = specialist is not null
                && session.Appointment!.SpecialistId == specialist.Id;

            if (!isAssignedSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "Only the assigned specialist can start recording.");
            }

            var recordingResult = ScreenRecording.Create(
                        session.Id,
                        RecordingAccessControl.Both,
                        RecordingStorageProvider.Agora);

            if (recordingResult.IsError)
                return recordingResult.Errors;

            var recording = recordingResult.Value;

            var result = session.StartRecording(recording);

            if (result.IsError)
                return result.Errors;

            _context.ScreenRecordings.Add(recording);

            await _context.SaveChangesAsync(ct);

            var setCurrentResult = session.SetCurrentRecording(recording);

            if (setCurrentResult.IsError)
                return setCurrentResult.Errors;

            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

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
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            var session = await _context.VideoSessions
                .Include(x => x.Appointment)
                .Include(x => x.CurrentRecording)
                .Include(x => x.Recordings)
                .FirstOrDefaultAsync(x => x.Id == videoSessionId, ct);

            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }

            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.Id, ct);

            var isAssignedSpecialist = specialist is not null
                && session.Appointment!.SpecialistId == specialist.Id;

            if (!isAssignedSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "Only the assigned specialist can stop recording.");
            }

            var stoppedRecording = session.CurrentRecording
                ?? session.Recordings.LastOrDefault();

            var result = session.StopRecording();

            if (result.IsError)
            {
                return result.Errors;
            }

            await _context.SaveChangesAsync(ct);

            var recordingDto = _mapper.Map<RecordingInfoDto>(stoppedRecording)
                ?? new RecordingInfoDto();

            recordingDto.Status = session.RecordingStatus?.ToString();
            recordingDto.StartedAtUtc = session.RecordingStartedAtUtc;
            recordingDto.CompletedAtUtc = session.RecordingCompletedAt;
            recordingDto.IsRecording = false;
            recordingDto.CanStartRecording = false;
            recordingDto.CanStopRecording = false;

            return Result<StopRecordingResponse>.Success(
                new StopRecordingResponse(session.Id, recordingDto));
        }

        public async Task<Result<RecordingInfoDto>> GetRecordingAsync(
            Guid videoSessionId,
            CancellationToken ct = default)
        {
            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            var session = await _context.VideoSessions
                .AsNoTracking()
                .Include(x => x.Appointment)
                .Include(x => x.CurrentRecording)
                .FirstOrDefaultAsync(x => x.Id == videoSessionId, ct);

            if (session is null)
            {
                return Error.NotFound(
                    "VideoSession.NotFound",
                    "Video session was not found.");
            }

            var appointment = session.Appointment!;
            var isClient = appointment.UserId == _currentUser.Id;

            var specialist = await _context.Specialists
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.Id, ct);

            var isSpecialist = specialist is not null
                && appointment.SpecialistId == specialist.Id;

            if (!isClient && !isSpecialist)
            {
                return Error.Forbidden(
                    "VideoSession.AccessDenied",
                    "You are not authorized to view recording info.");
            }

            var dto = _mapper.Map<RecordingInfoDto>(session.CurrentRecording)
                ?? new RecordingInfoDto();

            dto.Status = session.RecordingStatus?.ToString();
            dto.StartedAtUtc = session.RecordingStartedAtUtc;
            dto.CompletedAtUtc = session.RecordingCompletedAt;
            dto.IsRecording = session.IsRecording;
            dto.CurrentRecordingId = session.CurrentRecordingId;
            dto.Url ??= session.RecordingUrl;

            dto.CanStartRecording = isSpecialist
                && session.Status == Domain.Enums.VideoSessionStatus.Active
                && session.RecordingStatus is null;

            dto.CanStopRecording = isSpecialist
                && session.IsRecording;

            return Result<RecordingInfoDto>.Success(dto);
        }
    }
}
