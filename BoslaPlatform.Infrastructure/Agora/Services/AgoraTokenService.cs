using AgoraIO.Media;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Application.Interfaces.Authentication;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Application.Interfaces.Video;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Video;
using BoslaPlatform.Infrastructure.Settings;
using BoslaPlatform.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Error = BoslaPlatform.Shared.Error;

namespace BoslaPlatform.Infrastructure.Agora.Services
{
    /// <summary>
    /// Service for generating Agora RTC tokens for video session access.
    /// Uses the RtcTokenBuilder2 to create tokens with proper expiration and permissions.
    /// </summary>
    public class AgoraTokenService : IAgoraTokenService
    {
        private readonly AgoraSettings _settings;
        private readonly IAppDbContext _context;
        private readonly IUser _currentUser;

        /// <summary>
        /// Initializes a new instance of the AgoraTokenService.
        /// </summary>
        /// <param name="options">The Agora settings options.</param>
        /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
        public AgoraTokenService(
            IOptions<AgoraSettings> options,
            IAppDbContext context,
            IUser currentUser)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _settings = options.Value;
            _context = context;
            _currentUser = currentUser;

        }

        /// <summary>
        /// Generates an Agora RTC token for accessing a video session channel.
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A Result containing the AgoraTokenResponse with the generated token and session details.</returns>
        public async Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default)
        {
            

            if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
            {
                return Error.Unauthorized(
                    "User.Unauthorized",
                    "User is not authenticated.");
            }

            var appointment = await _context.Appointments
            .Include(x => x.VideoSession)
            .FirstOrDefaultAsync(
                x => x.Id == appointmentId,
                ct);

            if (appointment is null)
            {
                return Error.NotFound(
                    "Appointment.NotFound",
                    "Appointment was not found.");
            }

            //if (appointment.Status != AppointmentStatus.Confirmed)
            //{
            //    return Error.Validation(
            //        "Appointment.NotConfirmed",
            //        "Only confirmed appointments can start video sessions.");
            //}
            var specialist = await _context.Specialists
                .FirstOrDefaultAsync(
                    s => s.UserId == _currentUser.Id,
                    ct);

            var isSpecialist =
                specialist is not null &&
                appointment.SpecialistId == specialist.Id;

            var isClient =
                appointment.UserId == _currentUser.Id;

            if (!isSpecialist && !isClient)
            {
                return Error.Forbidden(
                    "Appointment.AccessDenied",
                    "You are not allowed to access this appointment.");
            }

            var videoSession = appointment.VideoSession;

            if (videoSession is null)
            {
                var sessionResult = VideoSession.Create(
                    appointment.Id,
                    $"bosla-appointment-{appointment.Id}",
                    _settings.AppId,
                    VideoSessionType.OneToOne);

                if (sessionResult.IsError)
                {
                    return sessionResult.Errors;
                }

                videoSession = sessionResult.Value;

                appointment.AttachVideoSession(videoSession);
                await _context.VideoSessions.AddAsync(
                    videoSession,
                    ct);
            }
            var uid = GenerateAgoraUid(
            _currentUser.Id.Value);

            var expiresAt = DateTimeOffset.UtcNow
                .AddMinutes(_settings.TokenExpirationMinutes);

            var privilegeExpiredTs =
                (uint)expiresAt.ToUnixTimeSeconds();

            var token = RtcTokenBuilder2.buildTokenWithUid(
                _settings.AppId,
                _settings.AppCertificate,
                videoSession.AgoraChannelName,
                uid,
                RtcTokenBuilder2.Role.RolePublisher,
                privilegeExpiredTs,
                privilegeExpiredTs);

            var role = isSpecialist
                ? VideoParticipantRole.Host
                : VideoParticipantRole.Participant;

            var alreadyJoined = videoSession.Participants
                .Any(x => x.UserId == _currentUser.Id);

            if (!alreadyJoined)
            {
                var participantResult = videoSession.AddParticipant(
                    _currentUser.Id.Value,
                    uid,
                    role);

                if (participantResult.IsError)
                {
                    return participantResult.Errors;
                }
            }

            await _context.SaveChangesAsync(ct);

            return Result<AgoraTokenResponse>.Success(
                new AgoraTokenResponse
                {
                    AppId = _settings.AppId,
                    ChannelName = videoSession.AgoraChannelName,
                    Token = token,
                    Uid = uid,
                    ExpiresAt = expiresAt
                });

            
        }
        private static uint GenerateAgoraUid(Guid userId)
        {
            return Math.Abs(userId.GetHashCode()) switch
            {
                var value => (uint)value
            };
        }
    }
}
