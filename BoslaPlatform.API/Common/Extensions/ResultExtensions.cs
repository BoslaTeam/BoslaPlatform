using BoslaPlatform.API.Common.Responses;
using BoslaPlatform.Shared;

namespace BoslaPlatform.API.Common.Extensions
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
