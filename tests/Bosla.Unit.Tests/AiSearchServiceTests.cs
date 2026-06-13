using Moq;
using Xunit;
using BoslaPlatform.Infrastructure.AI;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;

namespace Bosla.Unit.Tests;

public class AiSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsAnswer_FromChatService()
    {
        var embMock = new Mock<IEmbeddingService>();
        var vecMock = new Mock<IVectorStore>();
        var chatMock = new Mock<IChatService>();
        var dbMock = new Mock<IAppDbContext>();

        embMock.Setup(e => e.CreateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("[]");
        vecMock.Setup(v => v.SearchSimilarAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<(Guid, float)>());
        chatMock.Setup(c => c.ChatAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("stubbed-answer");

        var service = new AiSearchService(embMock.Object, vecMock.Object, chatMock.Object, dbMock.Object, new Mock<BoslaPlatform.Application.Interfaces.Authentication.IUser>().Object);

        var req = new SearchRequest { Query = "hello" };
        var res = await service.SearchAsync(req);

        Assert.NotNull(res);
        Assert.Equal("stubbed-answer", res.Answer);
    }
}
