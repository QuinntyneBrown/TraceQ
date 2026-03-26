namespace TraceQ.Infrastructure.VectorStore;

/// <summary>
/// Configuration options for connecting to a Qdrant vector database instance.
/// Bound from the "Qdrant" section in appsettings.json.
/// </summary>
public class QdrantOptions
{
    public const string SectionName = "Qdrant";

    /// <summary>
    /// Qdrant server hostname. Defaults to "localhost".
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Qdrant HTTP/REST port. Used for health checks if needed.
    /// </summary>
    public int HttpPort { get; set; } = 6333;

    /// <summary>
    /// Qdrant gRPC port. The Qdrant.Client library connects via gRPC.
    /// </summary>
    public int GrpcPort { get; set; } = 6334;

    /// <summary>
    /// Name of the Qdrant collection used for requirement vectors.
    /// </summary>
    public string CollectionName { get; set; } = "requirements";
}
