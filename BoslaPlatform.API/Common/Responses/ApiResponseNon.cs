using BoslaPlatform.Shared;

namespace BoslaPlatform.API.Common.Responses
{
    public sealed class ApiResponse
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public IReadOnlyList<Error>? Errors { get; init; }

        private ApiResponse()
        {
        }

        private ApiResponse(
            bool success,
            string message,
            IReadOnlyList<Error>? errors = null)
        {
            Success = success;
            Message = message;
            Errors = errors;
        }

        public static ApiResponse SuccessResponse(
            string message = "Request completed successfully")
        {
            return new(
                success: true,
                message: message);
        }

        public static ApiResponse FailureResponse(
            IReadOnlyList<Error> errors,
            string message = "Request failed")
        {
            return new(
                success: false,
                message: message,
                errors: errors);
        }
    }
}
