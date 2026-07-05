using BoslaPlatform.Shared;

namespace BoslaPlatform.API.Common.Responses
{
    public sealed class ApiResponse<T>
    {
        public bool Success { get; init; }

        public string Message { get; init; } = string.Empty;

        public T? Data { get; init; }

        public IReadOnlyList<Error>? Errors { get; init; }

        public PaginationMetadata? Pagination { get; init; }

        private ApiResponse()
        {
        }

        private ApiResponse(
            bool success,
            string message,
            T? data = default,
            IReadOnlyList<Error>? errors = null,
            PaginationMetadata? pagination = null)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
            Pagination = pagination;
        }

        public static ApiResponse<T> SuccessResponse(
            T data,
            string message = "Request completed successfully")
        {
            return new(
                success: true,
                message: message,
                data: data);
        }

        public static ApiResponse<T> FailureResponse(
            IReadOnlyList<Error> errors,
            string message = "Request failed")
        {
            return new(
                success: false,
                message: message,
                errors: errors);
        }

        public static ApiResponse<T> PaginatedResponse(
            T data,
            PaginationMetadata pagination,
            string message = "Request completed successfully")
        {
            return new(
                success: true,
                message: message,
                data: data,
                pagination: pagination);
        }
    }
}
