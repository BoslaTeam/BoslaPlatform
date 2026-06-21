using BoslaPlatform.Application.Features.VideoSessions.Interfaces;
using BoslaPlatform.Application.Features.VideoSessions.Responses;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Models.Booking;
using BoslaPlatform.Infrastructure.Agora.Interfaces;
using BoslaPlatform.Service.Interfaces.Authentication;
using BoslaPlatform.Service.Interfaces.Persistence;
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
        private readonly ICurrentUser _currentUser;
        private readonly IAgoraTokenService _agoraTokenService;

        /// <summary>
        /// Initializes a new instance of the VideoSessionService.
        /// </summary>
        /// <param name="context">The application database context.</param>
        /// <param name="currentUser">The current authenticated user context.</param>
        /// <param name="agoraTokenService">The Agora token generation service.</param>
        public VideoSessionService(
            IAppDbContext context,
            ICurrentUser currentUser,
            IAgoraTokenService agoraTokenService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _agoraTokenService = agoraTokenService ?? throw new ArgumentNullException(nameof(agoraTokenService));
        }

        /// <summary>
        /// Generates an Agora RTC token for a video session associated with an appointment.
        /// Validates that:
        /// - The appointment exists
        /// - The appointment is not cancelled
        /// - The current user is either the client or specialist in the appointment
        /// </summary>
        /// <param name="appointmentId">The ID of the appointment.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>A Result containing the AgoraTokenResponse or an appropriate error.</returns>
        public async Task<Result<AgoraTokenResponse>> GenerateTokenAsync(
            Guid appointmentId,
            CancellationToken ct = default)
        {
            // 1. Validate appointment exists
            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(x => x.Id == appointmentId, ct);

            if (appointment is null)
            {
                return Error.NotFound(
                    "Appointment.NotFound",
                    "The appointment was not found.");
            }

            // 2. Validate appointment is not cancelled
            if (appointment.Status == AppointmentStatus.Cancelled)
            {
                return Error.Validation(
                    "Appointment.Cancelled",
                    "Video sessions cannot be initiated for cancelled appointments.");
            }

            // 3. Validate current user is part of the appointment
            if (appointment.UserId != _currentUser.Id && appointment.SpecialistId != _currentUser.Id)
            {
                return Error.Forbidden(
                    "VideoSession.Unauthorized",
                    "You are not authorized to access this appointment's video session.");
            }

            // 4. Generate Agora token
            var tokenResult = await _agoraTokenService.GenerateTokenAsync(
                appointmentId,
                ct);

            return tokenResult;
        }
    }
}
