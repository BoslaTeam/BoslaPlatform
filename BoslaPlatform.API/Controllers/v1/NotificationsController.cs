using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Features.Notifications.Requests;
using BoslaPlatform.Application.Features.Notifications.Services;
using BoslaPlatform.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly INotificationPreferenceService _preferenceService;

        public NotificationsController(
            INotificationService notificationService,
            INotificationPreferenceService preferenceService)
        {
            _notificationService = notificationService;
            _preferenceService = preferenceService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetMyNotifications(CancellationToken ct)
        {
            var result = await _notificationService.GetMyAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<NotificationDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }
        [HttpGet("unread-count")]
        [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
        public async Task<IResult> GetUnreadCount(CancellationToken ct)
        {
            var result = await _notificationService.GetUnreadCountAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<int>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> MarkAsRead(Guid id, CancellationToken ct)
        {
            var result = await _notificationService.MarkReadAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Notification marked as read.")),
                errors => errors.ToProblem());
        }

        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> Delete(Guid id, CancellationToken ct)
        {
            var result = await _notificationService.DeleteAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Notification deleted.")),
                errors => errors.ToProblem());
        }

        [HttpPut("read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> MarkAllAsRead(CancellationToken ct)
        {
            var result = await _notificationService.MarkAllReadAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "All notifications marked as read.")),
                errors => errors.ToProblem());
        }

        [HttpGet("preferences")]
        [ProducesResponseType(typeof(ApiResponse<List<NotificationPreferenceDto>>), StatusCodes.Status200OK)]
        public async Task<IResult> GetPreferences(CancellationToken ct)
        {
            var result = await _preferenceService.GetMyAsync(ct);
            return result.Match(
                value => Results.Ok(ApiResponse<List<NotificationPreferenceDto>>.SuccessResponse(value)),
                errors => errors.ToProblem());
        }

        [HttpPut("preferences/{type}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> UpdatePreference(
            string type,
            [FromBody] UpdateNotificationPreferenceRequest request,
            CancellationToken ct)
        {
            if (!Enum.TryParse<NotificationType>(type, out var notificationType))
                return Results.BadRequest(ApiResponse<bool>.FailureResponse(
                    new List<BoslaPlatform.Shared.Error> { BoslaPlatform.Shared.Error.Create(BoslaPlatform.Shared.ErrorKind.Validation, "NotificationType", $"Invalid notification type: {type}") }));

            var result = await _preferenceService.UpdateAsync(notificationType, request.Enabled, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Preference updated.")),
                errors => errors.ToProblem());
        }
    }
}
