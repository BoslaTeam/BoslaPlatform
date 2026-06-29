# Qdrant Vector Store Connector

## Overview

The Qdrant Vector Store connector can be used to access and manage data in Qdrant.

## Supported Features

| Feature Area | Support |
|---|---|
| Collection maps to | Qdrant collection with payload indices for filterable data fields |
| Supported key property types | `ulong`, `Guid` |
| Supported data property types | `string`, `int`, `long`, `double`, `float`, `bool`, and enumerables of each |
| Supported vector property types | `ReadOnlyMemory<float>`, `Embedding<float>`, `float[]` |
| Supported index types | Hnsw |
| Supported distance functions | `CosineSimilarity`, `DotProductSimilarity`, `EuclideanDistance`, `ManhattanDistance` |
| Supported filter clauses | `AnyTagEqualTo`, `EqualTo` |
| Multiple vectors in a record | Yes (configurable) |
| `IsIndexed` supported? | Yes |
| `IsFullTextIndexed` supported? | Yes |
| `StorageName` supported? | Yes |
| HybridSearch supported? | Yes |

---

## Getting Started

### 1. Install the NuGet Package

```bash
dotnet add package Microsoft.SemanticKernel.Connectors.Qdrant --prerelease
```

### 2. Register with Dependency Injection

**Using Kernel Builder:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddQdrantVectorStore("localhost");
```

**Using IServiceCollection (ASP.NET Core):**

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddQdrantVectorStore("localhost");
```

**With explicit `QdrantClient` registration:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;

var kernelBuilder = Kernel.CreateBuilder();
kernelBuilder.Services.AddSingleton<QdrantClient>(sp => new QdrantClient("localhost"));
kernelBuilder.Services.AddQdrantVectorStore();
```

### 3. Direct Instantiation

**Vector Store:**

```csharp
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"), ownsClient: true);
```

**Named Collection:**

```csharp
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

var collection = new QdrantCollection<ulong, Hotel>(
    new QdrantClient("localhost"),
    "skhotels",
    ownsClient: true);
```

---

## Data Mapping

The connector maps data model properties to Qdrant's storage structure as follows:

- **Key** property → Qdrant point **id**
- **Data** properties → Qdrant point **payload**
- **Vector** properties → Qdrant point **vector(s)**

### Property Name Override (`StorageName`)

You can override storage field names using `StorageName` on data and vector properties.

> **Note:** Not supported for keys (fixed name in Qdrant) or unnamed single vectors.

**Example:**

```csharp
using Microsoft.Extensions.VectorData;

public class Hotel
{
    [VectorStoreKey]
    public ulong HotelId { get; set; }

    [VectorStoreData(IsIndexed = true, StorageName = "hotel_name")]
    public string HotelName { get; set; }

    [VectorStoreData(IsFullTextIndexed = true, StorageName = "hotel_description")]
    public string Description { get; set; }

    [VectorStoreVector(4, DistanceFunction = DistanceFunction.CosineSimilarity,
        IndexKind = IndexKind.Hnsw, StorageName = "hotel_description_embedding")]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}
```

**Resulting Qdrant storage:**

```json
{
    "id": 1,
    "payload": {
        "hotel_name": "Hotel Happy",
        "hotel_description": "A place where everyone can be happy."
    },
    "vector": {
        "hotel_description_embedding": [0.9, 0.1, 0.1, 0.1]
    }
}
```

---

## Qdrant Vector Modes

### Single Unnamed Vector (Default)

One vector per collection, stored without a name.

```csharp
new Hotel
{
    HotelId = 1,
    HotelName = "Hotel Happy",
    Description = "A place where everyone can be happy.",
    DescriptionEmbedding = new float[4] { 0.9f, 0.1f, 0.1f, 0.1f }
};
```

```json
{
    "id": 1,
    "payload": {
        "HotelName": "Hotel Happy",
        "Description": "A place where everyone can be happy."
    },
    "vector": [0.9, 0.1, 0.1, 0.1]
}
```

### Named Vectors

Multiple named vectors per point.

```csharp
new Hotel
{
    HotelId = 1,
    HotelName = "Hotel Happy",
    Description = "A place where everyone can be happy.",
    HotelNameEmbedding = new float[4] { 0.9f, 0.5f, 0.5f, 0.5f },
    DescriptionEmbedding = new float[4] { 0.9f, 0.1f, 0.1f, 0.1f }
};
```

```json
{
    "id": 1,
    "payload": {
        "HotelName": "Hotel Happy",
        "Description": "A place where everyone can be happy."
    },
    "vector": {
        "HotelNameEmbedding": [0.9, 0.5, 0.5, 0.5],
        "DescriptionEmbedding": [0.9, 0.1, 0.1, 0.1]
    }
}
```

**Enable named vectors mode:**

```csharp
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;

var vectorStore = new QdrantVectorStore(
    new QdrantClient("localhost"),
    ownsClient: true,
    new() { HasNamedVectors = true });

var collection = new QdrantCollection<ulong, Hotel>(
    new QdrantClient("localhost"),
    "skhotels",
    ownsClient: true,
    new() { HasNamedVectors = true });
```
