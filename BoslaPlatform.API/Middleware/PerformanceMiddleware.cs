using System.Diagnostics;

namespace BoslaPlatform.API.Middleware
{
    public class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMiddleware> _logger;

        private const int SlowRequestThresholdMs = 500;

        public PerformanceMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();

            await _next(context);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
            {
                var request = context.Request;

                _logger.LogWarning(
                    "Slow Request: {Method} {Path} took {ElapsedMs} ms",
                    request.Method,
                    request.Path,
                    stopwatch.ElapsedMilliseconds);
            }
        }

    }

}
