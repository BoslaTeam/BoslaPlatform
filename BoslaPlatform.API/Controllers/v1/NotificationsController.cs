using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Notifications.DTOs;
using BoslaPlatform.Application.Features.Notifications.Services;
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

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
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

        [HttpPut("{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IResult> MarkAsRead(Guid id, CancellationToken ct)
        {
            var result = await _notificationService.MarkReadAsync(id, ct);
            return result.Match(
                value => Results.Ok(ApiResponse<bool>.SuccessResponse(value, "Notification marked as read.")),
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
    }
}
