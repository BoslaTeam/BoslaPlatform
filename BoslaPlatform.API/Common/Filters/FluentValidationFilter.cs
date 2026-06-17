using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BoslaPlatform.API.Common.Filters
{
    public class FluentValidationFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            foreach (var (key, value) in context.ActionArguments)
            {
                if (value is null) continue;

                var validatorType = typeof(IValidator<>).MakeGenericType(value.GetType());
                var validator = context.HttpContext.RequestServices.GetService(validatorType) as IValidator;

                if (validator is null) continue;

                var validationContext = new ValidationContext<object>(value);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray());

                    context.Result = new BadRequestObjectResult(
                        new ValidationProblemDetails(errors)
                        {
                            Status = StatusCodes.Status400BadRequest
                        });
                    return;
                }
            }

            await next();
        }
    }
}
