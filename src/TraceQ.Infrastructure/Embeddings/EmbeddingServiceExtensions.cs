using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

        // Register embedding service as singleton (InferenceSession is thread-safe for reads)
        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<EmbeddingModelOptions>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var modelOptions = options.Value;

            if (!File.Exists(modelOptions.ModelPath) || !File.Exists(modelOptions.VocabPath))
            {
                return new FallbackEmbeddingService(
                    options,
                    loggerFactory.CreateLogger<FallbackEmbeddingService>());
            }

            try
            {
                return new OnnxEmbeddingService(
                    options,
                    loggerFactory.CreateLogger<OnnxEmbeddingService>());
            }
            catch (Exception ex)
            {
                loggerFactory.CreateLogger<FallbackEmbeddingService>().LogError(
                    ex,
                    "Failed to initialize the ONNX embedding service. Falling back to zero-vector embeddings.");

                return new FallbackEmbeddingService(
                    options,
                    loggerFactory.CreateLogger<FallbackEmbeddingService>());
            }
        });

        // Register background service for periodic embedding of new requirements
        services.AddHostedService<EmbeddingBackgroundService>();

        return services;
    }
}
