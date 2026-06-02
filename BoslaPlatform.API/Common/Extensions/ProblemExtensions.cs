using BoslaPlatform.Shared;
using Microsoft.AspNetCore.Mvc;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace BoslaPlatform.API.Common.Extensions
{
    public static class ProblemExtensions
    {
        public static IResult ToProblem(this IReadOnlyList<Error> errors)
        {
            if (errors.Count == 0)
            {
                return Results.Problem();
            }

            if (errors.All(error => error.Type == ErrorKind.Validation))
            {
                return ValidationProblem(errors);
            }

            return Problem(errors[0]);
        }

        private static IResult Problem(Error error)
        {
            var statusCode = error.Type switch
            {
                ErrorKind.Conflict => StatusCodes.Status409Conflict,
                ErrorKind.Validation => StatusCodes.Status400BadRequest,
                ErrorKind.NotFound => StatusCodes.Status404NotFound,
                ErrorKind.Unauthorized => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status500InternalServerError,
            };

            return Results.Problem(statusCode: statusCode, title: error.Description);
        }

        private static IResult ValidationProblem(IReadOnlyList<Error> errors)
        {
            var errorsDict = errors.ToDictionary(e => e.Code, e => new[] { e.Description });

            var problemDetails = new ValidationProblemDetails(errorsDict)
            {
                Status = StatusCodes.Status400BadRequest
            };

            return Results.Json(problemDetails, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
