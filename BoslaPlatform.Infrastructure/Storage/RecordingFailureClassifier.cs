using Amazon.S3;
using BoslaPlatform.Domain.Enums;

namespace BoslaPlatform.Infrastructure.Storage;

/// <summary>
/// Classifies exceptions into <see cref="RecordingFailureCategory"/> values
/// to drive the retry policy: only Transient and Network failures are retried.
/// </summary>
public static class RecordingFailureClassifier
{
    /// <summary>
    /// Returns true if this category is safe to retry.
    /// </summary>
    public static bool IsRetriable(RecordingFailureCategory category)
        => category is RecordingFailureCategory.Transient or RecordingFailureCategory.Network;

    /// <summary>
    /// Classifies an exception into a <see cref="RecordingFailureCategory"/>.
    /// </summary>
    public static RecordingFailureCategory Classify(Exception ex)
    {
        return ex switch
        {
            AmazonS3Exception s3ex when
                s3ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or
                                   System.Net.HttpStatusCode.Forbidden
                => RecordingFailureCategory.Authentication,

            AmazonS3Exception s3ex when
                s3ex.StatusCode is System.Net.HttpStatusCode.NotFound or
                                   System.Net.HttpStatusCode.BadRequest or
                                   System.Net.HttpStatusCode.MethodNotAllowed or
                                   System.Net.HttpStatusCode.Conflict
                => RecordingFailureCategory.Permanent,

            AmazonS3Exception s3ex when
                s3ex.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable or
                                   System.Net.HttpStatusCode.TooManyRequests or
                                   System.Net.HttpStatusCode.InternalServerError or
                                   System.Net.HttpStatusCode.GatewayTimeout
                => RecordingFailureCategory.Transient,

            AmazonS3Exception
                => RecordingFailureCategory.Storage,

            HttpRequestException
                => RecordingFailureCategory.Network,

            OperationCanceledException
                => RecordingFailureCategory.Transient,

            // Conservative default — treat as transient to maximise recovery chances
            _   => RecordingFailureCategory.Transient
        };
    }

    /// <summary>
    /// Classifies a failure from an error description string
    /// (used when the original exception is unavailable).
    /// </summary>
    public static RecordingFailureCategory ClassifyFromErrorCode(string errorCode)
    {
        if (errorCode.Contains("403") || errorCode.Contains("401") ||
            errorCode.Contains("Forbidden") || errorCode.Contains("Unauthorized"))
            return RecordingFailureCategory.Authentication;

        if (errorCode.Contains("400") || errorCode.Contains("404") ||
            errorCode.Contains("BadRequest") || errorCode.Contains("NotFound"))
            return RecordingFailureCategory.Permanent;

        if (errorCode.Contains("Storage") || errorCode.Contains("Bucket") ||
            errorCode.Contains("Quota"))
            return RecordingFailureCategory.Storage;

        if (errorCode.Contains("Network") || errorCode.Contains("Connection") ||
            errorCode.Contains("Timeout") || errorCode.Contains("HttpRequest"))
            return RecordingFailureCategory.Network;

        return RecordingFailureCategory.Transient;
    }
}
