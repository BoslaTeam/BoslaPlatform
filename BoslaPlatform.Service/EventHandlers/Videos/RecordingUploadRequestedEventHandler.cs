using BoslaPlatform.Application.Features.RecordingTransfer.Services;
using BoslaPlatform.Domain.Events.Videos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BoslaPlatform.Application.EventHandlers.Videos
{
    public sealed class RecordingUploadRequestedEventHandler
    : INotificationHandler<RecordingUploadRequestedEvent>
    {
        private readonly RecordingTransferService _transferService;
        private readonly ILogger<RecordingUploadRequestedEventHandler> _logger;

        public RecordingUploadRequestedEventHandler(
            RecordingTransferService transferService,
            ILogger<RecordingUploadRequestedEventHandler> logger)
        {
            _transferService = transferService;
            _logger = logger;
        }

        public async Task Handle(
            RecordingUploadRequestedEvent notification,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "Recording upload requested for session {SessionId}, resourceId={ResourceId}, sid={Sid}",
                notification.SessionId,
                notification.ResourceId,
                notification.Sid);

            await _transferService.TransferRecordingAsync(
                notification.SessionId,
                notification.ResourceId ?? string.Empty,
                notification.Sid ?? string.Empty,
                cancellationToken);
        }
    }
}