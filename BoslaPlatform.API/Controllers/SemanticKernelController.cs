//using Microsoft.AspNetCore.Mvc;
//using BoslaPlatform.Infrastructure.AI.SemanticKernel;
//using Microsoft.Extensions.Logging;
//using Swashbuckle.AspNetCore.Annotations;

//namespace BoslaPlatform.API.Controllers;

///// <summary>
///// Demo controller showcasing Semantic Kernel v1.77 integration with Gemini and Qdrant
///// </summary>
//[ApiController]
//[Route("api/[controller]")]
//public class SemanticKernelController : ControllerBase
//{
//    private readonly GeminiKernelOrchestrator _orchestrator;
//    private readonly QdrantMemoryService _memoryService;
//    private readonly ILogger<SemanticKernelController> _logger;

//    public SemanticKernelController(
//        GeminiKernelOrchestrator orchestrator,
//        QdrantMemoryService memoryService,
//        ILogger<SemanticKernelController> logger)
//    {
//        _orchestrator = orchestrator;
//        _memoryService = memoryService;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Ask Gemini a question via Semantic Kernel
//    /// </summary>
//    /// <param name="prompt">The question to ask</param>
//    /// <returns>Gemini's response</returns>
//    [HttpPost("ask")]
//    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> AskGeminiAsync([FromBody] AskRequest request)
//    {
//        try
//        {
//            var args = new Dictionary<string, object> { { "prompt", request.Prompt } };
//            var response = await _orchestrator.InvokeFunctionAsync("gemini", "ask_gemini", args);
//            return Ok(new AskResponse { Response = response });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error asking Gemini");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Embed text using Gemini via Semantic Kernel
//    /// </summary>
//    /// <param name="text">Text to embed</param>
//    /// <returns>Embedding result</returns>
//    [HttpPost("embed")]
//    [ProducesResponseType(typeof(EmbedResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> EmbedTextAsync([FromBody] EmbedRequest request)
//    {
//        try
//        {
//            var args = new Dictionary<string, object> { { "text", request.Text } };
//            var response = await _orchestrator.InvokeFunctionAsync("gemini", "embed_text", args);
//            return Ok(new EmbedResponse { Embedding = response });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error embedding text");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Store a memory vector in Qdrant
//    /// </summary>
//    [HttpPost("memory/store")]
//    [ProducesResponseType(typeof(StoreMemoryResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> StoreMemoryAsync([FromBody] StoreMemoryRequest request)
//    {
//        try
//        {
//            var args = new Dictionary<string, object>
//            {
//                { "id", request.Id },
//                { "embeddingJson", request.EmbeddingJson },
//                { "metadata", request.Metadata ?? "{}" }
//            };
//            var response = await _orchestrator.InvokeFunctionAsync("qdrant_memory", "store_memory", args);
//            return Ok(new StoreMemoryResponse { Message = response });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error storing memory");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Search similar memories in Qdrant
//    /// </summary>
//    [HttpPost("memory/search")]
//    [ProducesResponseType(typeof(SearchMemoryResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> SearchMemoryAsync([FromBody] SearchMemoryRequest request)
//    {
//        try
//        {
//            var args = new Dictionary<string, object>
//            {
//                { "queryEmbeddingJson", request.QueryEmbeddingJson },
//                { "topK", request.TopK ?? 5 }
//            };
//            var response = await _orchestrator.InvokeFunctionAsync("qdrant_memory", "search_memory", args);
//            return Ok(new SearchMemoryResponse { Results = response });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error searching memory");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Store text with automatic Gemini embedding to Qdrant memory
//    /// </summary>
//    [HttpPost("sk-memory/store-text")]
//    [ProducesResponseType(typeof(StoreTextResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> StoreTextAsync([FromBody] StoreTextRequest request)
//    {
//        try
//        {
//            var result = await _memoryService.StoreTextAsync(
//                text: request.Text,
//                id: request.Id,
//                metadata: request.Metadata,
//                cancellationToken: HttpContext.RequestAborted);

//            return Ok(new StoreTextResponse { Message = result });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error storing text with embedding");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Search Qdrant with automatic Gemini embedding of query
//    /// </summary>
//    [HttpPost("sk-memory/search")]
//    [ProducesResponseType(typeof(SearchTextResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> SearchTextAsync([FromBody] SearchTextRequest request)
//    {
//        try
//        {
//            var results = await _memoryService.SearchAsync(
//                query: request.Query,
//                topK: request.TopK ?? 10,
//                cancellationToken: HttpContext.RequestAborted);

//            return Ok(new SearchTextResponse 
//            { 
//                Results = results.Select(r => new SearchResultDto { Id = r.Id, Score = r.Score, Text = r.Text }).ToList()
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error searching with text query");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }

//    /// <summary>
//    /// Batch store multiple texts with automatic embeddings
//    /// </summary>
//    [HttpPost("sk-memory/batch-store")]
//    [ProducesResponseType(typeof(BatchStoreResponse), StatusCodes.Status200OK)]
//    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
//    public async Task<IActionResult> BatchStoreAsync([FromBody] BatchStoreRequest request)
//    {
//        try
//        {
//            var entries = request.Entries.Select(e => 
//                (e.Text, e.Id, e.Metadata)).ToList();

//            var results = await _memoryService.StoreBatchAsync(entries, HttpContext.RequestAborted);
//            return Ok(new BatchStoreResponse { Count = results.Count, Ids = results });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error batch storing texts");
//            return BadRequest(new ErrorResponse { Error = ex.Message });
//        }
//    }
//}

///// <summary>
///// Response from ask_gemini endpoint
///// </summary>
//public class AskResponse
//{
//    public string Response { get; set; } = string.Empty;
//}

//public class AskRequest
//{
//    public string Prompt { get; set; } = string.Empty;
//}

///// <summary>
///// Response from embed_text endpoint
///// </summary>
//public class EmbedResponse
//{
//    public string Embedding { get; set; } = string.Empty;
//}

//public class EmbedRequest
//{
//    public string Text { get; set; } = string.Empty;
//}

///// <summary>
///// Response from memory/store endpoint
///// </summary>
//public class StoreMemoryResponse
//{
//    public string Message { get; set; } = string.Empty;
//}

//public class StoreMemoryRequest
//{
//    public string Id { get; set; } = string.Empty;
//    public string EmbeddingJson { get; set; } = string.Empty;
//    public string? Metadata { get; set; }
//}

///// <summary>
///// Response from memory/search endpoint
///// </summary>
//public class SearchMemoryResponse
//{
//    public string Results { get; set; } = string.Empty;
//}

//public class SearchMemoryRequest
//{
//    public string QueryEmbeddingJson { get; set; } = string.Empty;
//    public int? TopK { get; set; }
//}

///// <summary>
///// Response from sk-memory/store-text endpoint
///// </summary>
//public class StoreTextResponse
//{
//    public string Message { get; set; } = string.Empty;
//}

///// <summary>
///// Store text with automatic embedding
///// </summary>
//public class StoreTextRequest
//{
//    public string Text { get; set; } = string.Empty;
//    public string? Id { get; set; }
//    public Dictionary<string, object>? Metadata { get; set; }
//}

///// <summary>
///// Search with text query (automatic embedding)
///// </summary>
//public class SearchTextRequest
//{
//    public string Query { get; set; } = string.Empty;
//    public int? TopK { get; set; }
//}

///// <summary>
///// Response from sk-memory/batch-store endpoint
///// </summary>
//public class BatchStoreResponse
//{
//    public int Count { get; set; }
//    public List<string> Ids { get; set; } = new();
//}

///// <summary>
///// Batch store multiple texts
///// </summary>
//public class BatchStoreRequest
//{
//    public List<BatchStoreEntry> Entries { get; set; } = new();
//}

//public class BatchStoreEntry
//{
//    public string Text { get; set; } = string.Empty;
//    public string? Id { get; set; }
//    public Dictionary<string, object>? Metadata { get; set; }
//}

///// <summary>
///// Search result from Qdrant
///// </summary>
//public class SearchResultDto
//{
//    public Guid Id { get; set; }
//    public float Score { get; set; }
//    public string? Text { get; set; }
//}

///// <summary>
///// Search text response
///// </summary>
//public class SearchTextResponse
//{
//    public List<SearchResultDto> Results { get; set; } = new();
//}

///// <summary>
///// Error response
///// </summary>
//public class ErrorResponse
//{
//    public string Error { get; set; } = string.Empty;
//}
