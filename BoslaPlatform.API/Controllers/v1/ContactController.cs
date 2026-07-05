using Asp.Versioning;
using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Application.Features.Contact.Requests;
using BoslaPlatform.Application.Interfaces.Communication;
using Microsoft.AspNetCore.Mvc;

namespace BoslaPlatform.API.Controllers.v1
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/contact")]
    public class ContactController : ControllerBase
    {
        private readonly IContactService _contactService;

        public ContactController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Submit([FromBody] ContactRequest request, CancellationToken ct)
        {
            await _contactService.HandleContactAsync(request, ct);
            return Ok(ApiResponse.SuccessResponse("تم استلام رسالتك بنجاح"));
        }
    }
}
