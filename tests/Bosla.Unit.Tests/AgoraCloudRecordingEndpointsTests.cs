using BoslaPlatform.Infrastructure.Recording.Providers.Agora.Configuration;
using Xunit;

namespace Bosla.Unit.Tests;

public class AgoraCloudRecordingEndpointsTests
{
    private const string BaseUrl = "https://api.agora.io";
    private const string AppId = "test-app";
    private const string ResourceId = "res-123";
    private const string Sid = "sid-456";
    private const string Mode = "mix";

    [Fact]
    public void BuildAcquireEndpoint_generates_correct_url()
    {
        var url = AgoraCloudRecordingEndpoints.BuildAcquireEndpoint(BaseUrl, AppId);
        Assert.Equal("https://api.agora.io/v1/apps/test-app/cloud_recording/acquire", url);
    }

    [Fact]
    public void BuildStartEndpoint_generates_correct_url_with_mode()
    {
        var url = AgoraCloudRecordingEndpoints.BuildStartEndpoint(BaseUrl, AppId, ResourceId, Mode);
        Assert.Equal("https://api.agora.io/v1/apps/test-app/cloud_recording/resourceid/res-123/mode/mix/start", url);
    }

    [Fact]
    public void BuildStopEndpoint_generates_correct_url_with_mode()
    {
        var url = AgoraCloudRecordingEndpoints.BuildStopEndpoint(BaseUrl, AppId, ResourceId, Sid, Mode);
        Assert.Equal("https://api.agora.io/v1/apps/test-app/cloud_recording/resourceid/res-123/sid/sid-456/mode/mix/stop", url);
    }

    [Fact]
    public void BuildQueryEndpoint_generates_correct_url_with_mode()
    {
        var url = AgoraCloudRecordingEndpoints.BuildQueryEndpoint(BaseUrl, AppId, ResourceId, Sid, Mode);
        Assert.Equal("https://api.agora.io/v1/apps/test-app/cloud_recording/resourceid/res-123/sid/sid-456/mode/mix/query", url);
    }

    [Fact]
    public void BuildReleaseEndpoint_generates_correct_url()
    {
        var url = AgoraCloudRecordingEndpoints.BuildReleaseEndpoint(BaseUrl, AppId, ResourceId);
        Assert.Equal("https://api.agora.io/v1/apps/test-app/cloud_recording/resourceid/res-123/release", url);
    }
}
