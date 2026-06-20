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
    [Route("api/v{version:apiVersion}/conversations")]
    [Authorize]

    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationsController(
            IConversationService conversationService)
        {
            _conversationService = conversationService;
        }
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status400BadRequest)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict)]
        [EndpointName("CreateConversation")]
        [EndpointSummary("Creates a conversation for a confirmed appointment.")]
        [EndpointDescription(
    "Creates a new conversation between the appointment participants. " +
    "The appointment must be confirmed and no conversation should already exist.")]
        [Tags("Communication")]
        public async Task<IResult> Create(CreateConversationRequest request,CancellationToken ct)
        {
            var result = await _conversationService
                .CreateAsync(request, ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<Guid>
                            .SuccessResponse(
                                value,
                                "Conversation created successfully.");

                    return Results.Created(
                        $"/api/v1/conversations/{value}",
                        response);
                },

                errors => errors.ToProblem());
        }
        [HttpGet("{id:guid}")]
        [EndpointName("GetConversationById")]
        [EndpointSummary("Retrieves a conversation by its identifier.")]
        [EndpointDescription(
    "Returns conversation details including participants and the latest message. " +
    "Only conversation participants can access this resource.")]
        [ProducesResponseType( typeof(ApiResponse<ConversationDto>),StatusCodes.Status200OK)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status404NotFound)]
        [Tags("Communication")]
        public async Task<IResult> GetById(Guid id,CancellationToken ct)
        {
            var result = await _conversationService.GetByIdAsync(id, ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<ConversationDto>
                            .SuccessResponse(
                                value,
                                "Conversation retrieved successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }
        [HttpGet]
        [EndpointName("GetMyConversations")]
        [EndpointSummary("Retrieves conversations for the current user.")]
        [EndpointDescription(
    "Returns paginated conversations ordered by last activity. " +
    "Each conversation contains participant information and the latest message.")]
        [ProducesResponseType( typeof(ApiResponse<PaginatedResult<ConversationDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType( typeof(ProblemDetails),StatusCodes.Status401Unauthorized)]
        [Tags("Communication")]
        public async Task<IResult> GetMy([FromQuery] PaginationRequest request,CancellationToken ct)
        {
            var result = await _conversationService.GetMyConversationsAsync(request, ct);

            return result.Match(
                value =>
                {
                    var response =
                        ApiResponse<PaginatedResult<ConversationDto>>
                            .SuccessResponse(
                                value,
                                "Conversations retrieved successfully.");

                    return Results.Ok(response);
                },

                errors => errors.ToProblem());
        }

    }
}
