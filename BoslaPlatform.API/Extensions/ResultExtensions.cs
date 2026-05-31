using BoslaPlatform.Shared;

namespace BoslaPlatform.API.Extensions
{
    public static class ResultExtensions
    {
        public static ApiResponse<T> ToApiResponse<T>(
            this Result<T> result,
            string successMessage = "Success")
        {
            return result.Match(
                value => ApiResponse<T>.SuccessResponse(
                    value,
                    successMessage),

                errors => ApiResponse<T>.FailureResponse(
                    errors));
        }
    }
}
