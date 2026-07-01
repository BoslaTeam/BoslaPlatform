# Semantic Kernel v1.77 Integration for Gemini & Qdrant

This documentation covers the Semantic Kernel (SK) v1.77-alpha integration for Google Gemini chat/embedding and Qdrant vector memory.

## Overview

The SK adapter suite provides a modern, plugin-based architecture for orchestrating Gemini and Qdrant through Microsoft's Semantic Kernel framework.

### ✨ NEW: Native SK Qdrant Connector

**Microsoft.SemanticKernel.Connectors.Qdrant** is now integrated for seamless vector store operations:

- `QdrantMemoryService` - High-level memory abstraction backed by Qdrant Cloud
- Automatic embedding generation via Gemini for text storage and retrieval
- New HTTP endpoints for simplified workflows

#### Quick Start

```csharp
[Inject] QdrantMemoryService memory;

// Store text with automatic Gemini embedding
await memory.StoreTextAsync("Your text here", id: "doc-1");

// Search with automatic embedding
var results = await memory.SearchAsync("your query", topK: 10);
foreach (var (id, score, _) in results)
    Console.WriteLine($"{id}: {score:F2}");
```

#### New HTTP Endpoints

```http
# Store text with auto-embedding
POST /api/semantickernel/sk-memory/store-text
{ "text": "Machine learning basics", "id": "ml-101" }

# Search with auto-embedding
POST /api/semantickernel/sk-memory/search
{ "query": "what is machine learning", "topK": 5 }

# Batch store
POST /api/semantickernel/sk-memory/batch-store
{ "entries": [{"text": "text1"}, {"text": "text2"}] }
```

### Components

#### 1. **GeminiPlugin** (`Ai/SemanticKernel/GeminiPlugin.cs`)
Exposes Gemini chat and embedding services as SK kernel functions.

- `[KernelFunction("ask_gemini")]` - Chat with Gemini
- `[KernelFunction("embed_text")]` - Generate embeddings via Gemini

**Dependencies:**
- `IChatService` (GeminiChatService)
- `IEmbeddingService` (GeminiEmbeddingService)
- `ILogger<GeminiPlugin>`

**Example Usage:**
```csharp
var args = new Dictionary<string, object> { { "prompt", "What is AI?" } };
var result = await orchestrator.InvokeFunctionAsync("gemini", "ask_gemini", args);
```

#### 2. **QdrantMemoryService** (`Ai/SemanticKernel/QdrantMemoryService.cs`)
High-level memory abstraction combining Gemini embeddings with Qdrant storage.

**Methods:**
- `StoreTextAsync(text, id?, metadata?)` - Store text with automatic Gemini embedding
- `SearchAsync(query, topK)` - Search with automatic query embedding  
- `StoreBatchAsync(entries)` - Batch store multiple texts
- `GetQdrantClient()` - Direct access to QdrantClient for advanced operations

**Dependencies:**
- `QdrantClient` - Underlying vector store
- `IEmbeddingService` - Gemini embeddings
- `QdrantSettings` - Configuration

**Example:**
```csharp
var service = sp.GetRequiredService<QdrantMemoryService>();

// Store with auto-embedding
await service.StoreTextAsync(
	text: "Machine learning is a subset of AI",
	id: "ml-101",
	metadata: new() { { "category", "education" } });

// Search with auto-embedding
var results = await service.SearchAsync("what is machine learning", topK: 5);
foreach (var (id, score, _) in results)
	Console.WriteLine($"{id}: {score}");
```

#### 3. **QdrantMemoryPlugin** (`Ai/SemanticKernel/QdrantMemoryPlugin.cs`)
Exposes Qdrant vector database functions as SK kernel functions.

- `[KernelFunction("store_memory")]` - Upsert a vector to Qdrant
- `[KernelFunction("search_memory")]` - Search similar vectors

**Dependencies:**
- `QdrantClient`
- `QdrantSettings` (via IOptions)
- `ILogger<QdrantMemoryPlugin>`

**Example Usage:**
```csharp
var embeddings = new float[] { /* embedding vector */ };
var args = new Dictionary<string, object> 
{
	{ "id", Guid.NewGuid().ToString() },
	{ "embeddingJson", JsonSerializer.Serialize(embeddings) },
	{ "metadata", "{\"text\":\"sample\"}" }
};
var result = await orchestrator.InvokeFunctionAsync("qdrant_memory", "store_memory", args);
```

#### 4. **GeminiKernelOrchestrator** (`Ai/SemanticKernel/GeminiKernelOrchestrator.cs`)
Main orchestration class that manages the Kernel and plugin lifecycle.

- Registers both GeminiPlugin and QdrantMemoryPlugin
- Provides methods for invoking functions by name
- Handles error logging and exception management

**Methods:**
- `InvokeAsync(string promptText)` - Execute a prompt
- `InvokeFunctionAsync(string pluginName, string functionName, Dictionary<string, object>? args)` - Invoke a specific function

#### 5. **SemanticKernelExtensions** (`Ai/SemanticKernel/SemanticKernelExtensions.cs`)
Extension method for DI registration.

```csharp
services.AddSemanticKernelForGemini();
```

Registers:
- `Kernel` (scoped)
- `QdrantMemoryService` (scoped)
- `GeminiPlugin` (scoped)
- `QdrantMemoryPlugin` (scoped)
- `GeminiKernelOrchestrator` (scoped)

## Integration with DI

In `BoslaPlatform.Infrastructure/DependencyInjection.cs`, the SK extension is called in the Gemini provider block:

```csharp
if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
{
	// ... Gemini settings ...
	services.AddSemanticKernelForGemini();
}
```

**Prerequisites:**
- `appsettings.json` must have `AI:Provider` set to `"Gemini"`
- `GeminiSettings` and `QdrantSettings` must be configured

## API Controller

`BoslaPlatform.API/Controllers/SemanticKernelController.cs` provides HTTP endpoints for SK function invocation:

### Endpoints

#### Ask Gemini
```http
POST /api/semantickernel/ask
Content-Type: application/json

{ "prompt": "What is machine learning?" }
```

Response:
```json
{
  "response": "Machine learning is..."
}
```

#### Embed Text
```http
POST /api/semantickernel/embed
Content-Type: application/json

{ "text": "Sample text to embed" }
```

Response:
```json
{
  "embedding": "[0.1, 0.2, 0.3, ...]"
}
```

#### Store Memory
```http
POST /api/semantickernel/memory/store
Content-Type: application/json

{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "embeddingJson": "[0.1, 0.2, 0.3, ...]",
  "metadata": "{\"text\":\"sample\"}"
}
```

#### Search Memory
```http
POST /api/semantickernel/memory/search
Content-Type: application/json

{
  "queryEmbeddingJson": "[0.1, 0.2, 0.3, ...]",
  "topK": 5
}
```

Response:
```json
{
  "results": "[{\"id\":\"...\",\"score\":0.95}, ...]"
}
```

## Plugin Architecture (SK v1.77)

### Key Differences from Earlier SK Versions

- **No ITextGeneration / ITextEmbeddingGeneration**: These interfaces are removed in v1.77+
- **Plugin-based Extensibility**: Use `[KernelFunction]` attributes on public methods
- **Simple Registration**: `kernel.Plugins.AddFromObject(plugin, "pluginName")`

### Function Signature

```csharp
[KernelFunction("function_name")]
[Description("Human-readable description")]
public async Task<string> FunctionAsync(
	[Description("Parameter description")] string param1,
	[Description("Another parameter")] string param2)
{
	// Implementation
	return result;
}
```

## Testing

Unit tests are provided in `BoslaPlatform.Tests/Infrastructure/AI/SemanticKernel/SemanticKernelPluginTests.cs`:

- `AddSemanticKernelForGemini_Should_Register_All_Services()` - Verifies DI registration
- `GeminiKernelOrchestrator_Should_Register_Plugins_In_Kernel()` - Validates plugin registration
- `GeminiPlugin_Should_Expose_KernelFunctions()` - Checks Gemini functions are available
- `QdrantMemoryPlugin_Should_Expose_KernelFunctions()` - Checks Qdrant functions are available

**Run tests:**
```bash
dotnet test BoslaPlatform.Tests --filter "SemanticKernelPluginTests"
```

## Configuration

### appsettings.json

```json
{
  "AI": {
	"Provider": "Gemini"
  },
  "GeminiSettings": {
	"ApiKey": "your-gemini-api-key",
	"Model": "gemini-1.5-pro-latest",
	"EmbeddingModel": "text-embedding-004"
  },
  "QdrantSettings": {
	"Url": "https://your-qdrant-cloud.qdrant.io",
	"ApiKey": "your-qdrant-api-key",
	"CollectionName": "embeddings"
  }
}
```

## Error Handling

All plugins log errors via `ILogger<T>`. The orchestrator provides exception context:

```csharp
catch (Exception ex)
{
	_logger.LogError(ex, "Error invoking {Plugin}.{Function}", pluginName, functionName);
	throw;
}
```

## Performance Considerations

- **Plugins are Singletons**: Shared across requests; thread-safe
- **Kernel is Singleton**: Avoid creating multiple kernel instances
- **Async-Only**: All function invocations are asynchronous
- **Batch Operations**: GeminiEmbeddingService supports batching (see Gemini integration docs)

## Future Enhancements

- Add native SK connectors (Microsoft.SemanticKernel.Connectors.Google)
- Implement SK memory abstractions with Qdrant backend
- Add prompt templates for common AI patterns
- Integrate SK agents for complex orchestration

## Troubleshooting

### Plugin Not Found
**Error:** `System.Collections.Generic.KeyNotFoundException: Plugin 'gemini' not found`

**Solution:** Ensure `GeminiKernelOrchestrator` is fully initialized in DI before use.

### Function Invocation Timeout
**Error:** `TaskCanceledException` when calling a function

**Solution:** Check Gemini/Qdrant service availability and network connectivity. Increase timeout in `GeminiHttpClient`.

### Missing [KernelFunction] Attribute
**Error:** Functions not visible in kernel plugins

**Solution:** Ensure methods have `[KernelFunction("name")]` and are public.

## References

- [Semantic Kernel v1.77 Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Google Gemini API](https://ai.google.dev/)
- [Qdrant Cloud](https://qdrant.tech/)
