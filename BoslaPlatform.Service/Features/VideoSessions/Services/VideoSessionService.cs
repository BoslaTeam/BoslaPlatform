using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Application.Features.VideoSessions.Services
{
    /// <summary>
    /// Service implementation for managing video sessions and generating Agora tokens.
    /// Validates appointments and coordinates with the Agora token service for token generation.
    /// </summary>
    public class VideoSessionService : IVideoSessionService
    {
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;
        private readonly IAgoraTokenService _agoraTokenService;

        /// <summary>
        /// Initializes a new instance of the VideoSessionService.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="currentUser">The current authenticated user context.</param>
        /// <param name="agoraTokenService">The Agora token generation service.</param>
        public VideoSessionService(IAppDbContext context,IUser currentUser,IAgoraTokenService agoraTokenService)
        {
            _context = context;
            _currentUser = currentUser;
            _agoraTokenService = agoraTokenService;
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
        /// Starts a video session.
        /// </summary>
        /// <param name="videoSessionId">
        /// Video session identifier.
        /// </param>
        /// <param name="ct">
        /// Cancellation token.
        /// </param>
        /// <returns>
        /// Started session details.
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

            //var validation =session.Appointment!.CanStartVideoSession(DateTimeOffset.UtcNow);

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
        /// Ends a video session.
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
                    "Only the assigned specialist can start this session.");
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

        //private static uint GenerateAgoraUid(Guid userId)
        //{
        //    return Math.Abs(userId.GetHashCode()) switch
        //    {
        //        var value => (uint)value
        //    };
        //}
    }
}
