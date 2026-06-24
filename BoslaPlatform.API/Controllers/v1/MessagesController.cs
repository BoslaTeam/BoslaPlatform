using Asp.Versioning;
using BoslaPlatform.API.Common.Extensions;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Conversations.Dtos;
using BoslaPlatform.Application.Features.Conversations.Requests;
using BoslaPlatform.Application.Interfaces.Conversation;
using BoslaPlatform.Shared.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/conversations/{conversationId:guid}/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }
        [HttpGet]
        [EndpointName("GetConversationMessages")]
        [EndpointSummary("Retrieves messages for a conversation.")]
        [EndpointDescription(
            "Returns paginated messages ordered by creation date. " +
            "Only conversation participants can access messages.")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<MessageDto>>),StatusCodes.Status200OK)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [Tags("Communication")]
        public async Task<IResult> Get(Guid conversationId, [FromQuery] PaginationRequest request,CancellationToken ct)
        {
            var result = await _messageService
                .GetAsync(
                    conversationId,
                    request,
                    ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<PaginatedResult<MessageDto>>
                            .SuccessResponse(
                                value,
                                "Messages retrieved successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }
        [HttpPost]
        [ProducesResponseType( typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [EndpointName("SendMessage")]
        [EndpointSummary("Sends a message to a conversation.")]
        [EndpointDescription(
    "Creates a new message and publishes MessageSentEvent. " +
    "All conversation participants receive the message through SignalR.")]
        [Tags("Communication")]
        public async Task<IResult> Send(Guid conversationId,SendMessageRequest request,CancellationToken ct)
        {
            var result = await _messageService
                .SendAsync(
                    conversationId,
                    request,
                    ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<Guid>
                            .SuccessResponse(
                                value,
                                "Message sent successfully.");

                    return Results.Created(
                        $"/api/v1/conversations/{conversationId}/messages/{value}",
                        response);
                },

                errors => errors.ToProblem());
        }
        [HttpPut("{messageId:guid}")]
        [EndpointName("EditMessage")]
        [EndpointSummary("Edits an existing message.")]
        [EndpointDescription(
    "Updates the content of a previously sent message. " +
    "Only the original sender can edit the message. " +
    "Publishing a MessageEditedEvent notifies connected clients.")]
        [ProducesResponseType(typeof(ApiResponse<bool>),StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [Tags("Communication")]
        public async Task<IResult> Edit(Guid conversationId,Guid messageId,EditMessageRequest request,CancellationToken ct)
        {
            var result = await _messageService
                .EditAsync(
                    conversationId,
                    messageId,
                    request,
                    ct);

            return result.Match(
                _ =>
                {
                    var response =
                        ApiResponse<bool>
                            .SuccessResponse(
                                true,
                                "Message updated successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }
        [HttpDelete("{messageId:guid}")]
        [EndpointName("DeleteMessage")]
        [EndpointSummary("Deletes an existing message.")]
        [EndpointDescription(
    "Removes a message from the conversation. " +
    "Only the original sender can delete the message. " +
    "Publishing a MessageDeletedEvent notifies connected clients.")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [Tags("Communication")]
        public async Task<IResult> Delete(Guid conversationId,Guid messageId,CancellationToken ct)
        {
            var result = await _messageService
                .DeleteAsync(
                    conversationId,
                    messageId,
                    ct);

            return result.Match(
                _ =>
                {
                    return Results.NoContent();
                },
                errors => errors.ToProblem());
        }
    }
}
