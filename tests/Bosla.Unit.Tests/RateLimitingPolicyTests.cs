using Xunit;
using Moq;
using BoslaPlatform.Infrastructure.RateLimiting;
using BoslaPlatform.Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace Bosla.Unit.Tests;

public class RateLimitPolicyTests
{
    private static readonly RateLimitPolicyOptions DefaultOptions = new()
    {
        PermitLimit = 100,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    private static Mock<IRateLimitPartitionResolver> CreateResolver(string partitionKey = "test-key")
    {
        var mock = new Mock<IRateLimitPartitionResolver>();
        mock.Setup(r => r.Resolve(It.IsAny<HttpContext>())).Returns(partitionKey);
        return mock;
    }

    private static HttpContext CreateHttpContext(string path, bool isAuthenticated = true)
    {
        var identity = new ClaimsIdentity(isAuthenticated ? "test" : null);
        if (isAuthenticated)
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "user-123"));
        var principal = new ClaimsPrincipal(identity);

        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.User).Returns(principal);
        mockContext.Setup(c => c.Items).Returns(new Dictionary<object, object?>());

        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Path).Returns(new PathString(path));
        mockRequest.Setup(r => r.Method).Returns("GET");
        mockContext.Setup(c => c.Request).Returns(mockRequest.Object);

        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(ci => ci.RemoteIpAddress).Returns(IPAddress.Parse("10.0.0.1"));
        mockContext.Setup(c => c.Connection).Returns(mockConnection.Object);

        return mockContext.Object;
    }

    [Fact]
    public void GetPartition_WithAuthenticatedUser_SetsItems()
    {
        var resolverMock = CreateResolver("resolved-key");
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, resolverMock.Object);
        var httpContext = CreateHttpContext("/api/test");

        policy.GetPartition(httpContext);

        Assert.Equal("TestPolicy", httpContext.Items[RateLimitPolicy.PolicyNameKey]);
        Assert.Equal("resolved-key", httpContext.Items[RateLimitPolicy.PartitionKeyKey]);
        Assert.Equal(100, httpContext.Items[RateLimitPolicy.PermitLimitKey]);
    }

    [Fact]
    public void GetPartition_WithExcludedPathHealth_DoesNotSetItems()
    {
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, CreateResolver().Object);
        var httpContext = CreateHttpContext("/health");

        policy.GetPartition(httpContext);

        var items = (IDictionary<object, object?>)httpContext.Items;
        Assert.False(items.ContainsKey(RateLimitPolicy.PolicyNameKey));
    }

    [Fact]
    public void GetPartition_WithExcludedPathSwagger_DoesNotSetItems()
    {
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, CreateResolver().Object);
        var httpContext = CreateHttpContext("/swagger/index.html");

        policy.GetPartition(httpContext);

        var items = (IDictionary<object, object?>)httpContext.Items;
        Assert.False(items.ContainsKey(RateLimitPolicy.PolicyNameKey));
    }

    [Fact]
    public void GetPartition_WithExcludedPathMetrics_DoesNotSetItems()
    {
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, CreateResolver().Object);
        var httpContext = CreateHttpContext("/metrics");

        policy.GetPartition(httpContext);

        var items = (IDictionary<object, object?>)httpContext.Items;
        Assert.False(items.ContainsKey(RateLimitPolicy.PolicyNameKey));
    }

    [Fact]
    public void GetPartition_WithExcludedPathCaseInsensitive_DoesNotSetItems()
    {
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, CreateResolver().Object);
        var httpContext = CreateHttpContext("/Health");

        policy.GetPartition(httpContext);

        var items = (IDictionary<object, object?>)httpContext.Items;
        Assert.False(items.ContainsKey(RateLimitPolicy.PolicyNameKey));
    }

    [Fact]
    public void GetPartition_OnRejected_ReturnsNull()
    {
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, CreateResolver().Object);

        Assert.Null(policy.OnRejected);
    }

    [Fact]
    public void GetPartition_WithAnonymousUser_SetsPartitionKeyFromResolver()
    {
        var resolverMock = CreateResolver("192.168.1.100");
        var policy = new RateLimitPolicy("TestPolicy", DefaultOptions, resolverMock.Object);
        var httpContext = CreateHttpContext("/api/test", isAuthenticated: false);

        policy.GetPartition(httpContext);

        Assert.Equal("192.168.1.100", httpContext.Items[RateLimitPolicy.PartitionKeyKey]);
    }

    [Fact]
    public void GetPartition_RespectsPolicyOptions()
    {
        var customOptions = new RateLimitPolicyOptions
        {
            PermitLimit = 50,
            WindowSeconds = 30,
            QueueLimit = 2
        };

        var policy = new RateLimitPolicy("CustomPolicy", customOptions, CreateResolver().Object);
        var httpContext = CreateHttpContext("/api/test");

        policy.GetPartition(httpContext);

        Assert.Equal(50, httpContext.Items[RateLimitPolicy.PermitLimitKey]);
    }
}
