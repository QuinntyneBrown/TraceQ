using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Embeddings;

/// <summary>
/// Extension methods for registering embedding services in the DI container.
/// </summary>
public static class EmbeddingServiceExtensions
{
    /// <summary>
    /// Registers the ONNX embedding service, tokenizer, and background embedding worker.
    /// The OnnxEmbeddingService is registered as a singleton because the InferenceSession
    /// is thread-safe for concurrent inference calls.
    /// </summary>
    public static IServiceCollection AddEmbeddingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        services.Configure<EmbeddingModelOptions>(
            configuration.GetSection(EmbeddingModelOptions.SectionName));

        // Register the production embedding service only. Startup should fail
        // immediately if the model files are missing or cannot be loaded.
        services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();

        // Register background service for periodic embedding of new requirements
        services.AddHostedService<EmbeddingBackgroundService>();

        return services;
    }
}
