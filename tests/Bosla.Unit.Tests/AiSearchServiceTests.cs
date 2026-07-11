using Moq;
using Xunit;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Infrastructure.AI;
using BoslaPlatform.Infrastructure.AI.Tokenizers;
using BoslaPlatform.Application.Interfaces.AI;
using BoslaPlatform.Application.Interfaces.Persistence;
using BoslaPlatform.Service.Features.AI.Requests;
using BoslaPlatform.Service.Features.AI.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Bosla.Unit.Tests;

public class AiSearchServiceTests
{
    private static Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data)
        where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        return mockSet;
    }

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

        dbMock.Setup(d => d.Set<Specialist>())
            .Returns(CreateMockDbSet(new List<Specialist>()).Object);

        var service = new AiSearchService(
            embMock.Object, vecMock.Object, chatMock.Object,
            dbMock.Object,
            new Mock<BoslaPlatform.Application.Interfaces.Authentication.IUser>().Object,
            Mock.Of<ITokenizer>(),
            Mock.Of<ILogger<AiSearchService>>());

        var req = new SearchRequest { Query = "hello" };
        var res = await service.SearchAsync(req);

        Assert.NotNull(res);
        Assert.Equal("stubbed-answer", res.Answer);
    }

    private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

        public T Current => _inner.Current;

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
