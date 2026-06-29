# Qdrant Data Mapping

The Qdrant connector provides a default mapper when mapping data from the data model to storage. Qdrant requires properties to be mapped into **id**, **payload**, and **vector(s)** groupings. The default mapper uses the model annotations or record definition to determine the type of each property and to do this mapping.

- The data model property annotated as a **key** will be mapped to the Qdrant point `id`.
- The data model properties annotated as **data** will be mapped to the Qdrant point `payload` object.
- The data model properties annotated as **vectors** will be mapped to the Qdrant point `vector` object.

---

## Property Name Override

For data properties and vector properties (if using named vectors mode), you can provide override field names to use in storage that differ from the property names on the data model. This is **not supported** for keys, since a key has a fixed name in Qdrant. It is also not supported for vectors in single unnamed vector mode, since the vector is stored under a fixed name.

The property name override is done by setting the `StorageName` option via the data model attributes or record definition.

Here is an example of a data model with `StorageName` set on its attributes and how that will be represented in Qdrant:

**C#**
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

    [VectorStoreVector(4, DistanceFunction = DistanceFunction.CosineSimilarity, IndexKind = IndexKind.Hnsw, StorageName = "hotel_description_embedding")]
    public ReadOnlyMemory<float>? DescriptionEmbedding { get; set; }
}
```

**JSON (Qdrant representation)**
```json
{
    "id": 1,
    "payload": { "hotel_name": "Hotel Happy", "hotel_description": "A place where everyone can be happy." },
    "vector": {
        "hotel_description_embedding": [0.9, 0.1, 0.1, 0.1]
    }
}
```

---

## Qdrant Vector Modes

Qdrant supports two modes for vector storage, and the Qdrant Connector with default mapper supports both. The default mode is **single unnamed vector**.

### Single Unnamed Vector

With this option, a collection may only contain a single vector and it will be unnamed in the storage model in Qdrant.

**C#**
```csharp
new Hotel
{
    HotelId = 1,
    HotelName = "Hotel Happy",
    Description = "A place where everyone can be happy.",
    DescriptionEmbedding = new float[4] { 0.9f, 0.1f, 0.1f, 0.1f }
};
```

**JSON (Qdrant representation)**
```json
{
    "id": 1,
    "payload": { "HotelName": "Hotel Happy", "Description": "A place where everyone can be happy." },
    "vector": [0.9, 0.1, 0.1, 0.1]
}
```

---

### Named Vectors

If using the named vectors mode, each point in a collection may contain more than one vector, and each will be named.

**C#**
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

**JSON (Qdrant representation)**
```json
{
    "id": 1,
    "payload": { "HotelName": "Hotel Happy", "Description": "A place where everyone can be happy." },
    "vector": {
        "HotelNameEmbedding": [0.9, 0.5, 0.5, 0.5],
        "DescriptionEmbedding": [0.9, 0.1, 0.1, 0.1]
    }
}
```

To enable named vectors mode, pass this as an option when constructing a Vector Store or collection. The same options can also be passed to any of the provided dependency injection container extension methods.

**C#**
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
