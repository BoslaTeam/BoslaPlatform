using Xunit;
using Moq;
using BoslaPlatform.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Security.Claims;

namespace Bosla.Unit.Tests;

public class DefaultRateLimitPartitionResolverTests
{
    private static DefaultRateLimitPartitionResolver CreateResolver() => new();

    private static HttpContext CreateHttpContext(
        bool isAuthenticated,
        string? nameIdentifier = null,
        string? sub = null,
        IPAddress? remoteIp = null)
    {
        var claims = new List<Claim>();
        if (nameIdentifier is not null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        if (sub is not null)
            claims.Add(new Claim("sub", sub));

        var identity = new ClaimsIdentity(claims, isAuthenticated ? "test" : null);
        var principal = new ClaimsPrincipal(identity);

        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.User).Returns(principal);

        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(ci => ci.RemoteIpAddress).Returns(remoteIp);
        mockContext.Setup(c => c.Connection).Returns(mockConnection.Object);

        return mockContext.Object;
    }

    [Fact]
    public void Resolve_WithAuthenticatedUserAndNameIdentifier_ReturnsUserId()
    {
        var httpContext = CreateHttpContext(
            isAuthenticated: true,
            nameIdentifier: "user-123");

        var result = CreateResolver().Resolve(httpContext);

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void Resolve_WithAuthenticatedUserAndSubClaim_ReturnsSub()
    {
        var httpContext = CreateHttpContext(
            isAuthenticated: true,
            sub: "sub-456");

        var result = CreateResolver().Resolve(httpContext);

        Assert.Equal("sub-456", result);
    }

    [Fact]
    public void Resolve_WithNameIdentifierPreferredOverSub_ReturnsNameIdentifier()
    {
        var httpContext = CreateHttpContext(
            isAuthenticated: true,
            nameIdentifier: "user-123",
            sub: "sub-456");

        var result = CreateResolver().Resolve(httpContext);

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void Resolve_WithAuthenticatedUserButNoClaims_ReturnsUnknownUser()
    {
        var identity = new ClaimsIdentity("test");
        var principal = new ClaimsPrincipal(identity);
        var mockContext = new Mock<HttpContext>();
        mockContext.Setup(c => c.User).Returns(principal);
        var mockConnection = new Mock<ConnectionInfo>();
        mockConnection.Setup(ci => ci.RemoteIpAddress).Returns((IPAddress?)null);
        mockContext.Setup(c => c.Connection).Returns(mockConnection.Object);

        var result = CreateResolver().Resolve(mockContext.Object);

        Assert.Equal("unknown-user", result);
    }

    [Fact]
    public void Resolve_WithAnonymousUser_ReturnsIpAddress()
    {
        var httpContext = CreateHttpContext(
            isAuthenticated: false,
            remoteIp: IPAddress.Parse("192.168.1.1"));

        var result = CreateResolver().Resolve(httpContext);

        Assert.Equal("192.168.1.1", result);
    }

    [Fact]
    public void Resolve_WithAnonymousUserAndNullIp_ReturnsUnknownIp()
    {
        var httpContext = CreateHttpContext(
            isAuthenticated: false);

        var result = CreateResolver().Resolve(httpContext);

        Assert.Equal("unknown-ip", result);
    }
}
