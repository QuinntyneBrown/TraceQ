using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qdrant.Client;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure.Csv;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Services;
using TraceQ.Infrastructure.VectorStore;

namespace TraceQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTraceQInfrastructure(this IServiceCollection services, string? connectionString = null)
    {
        // Register DbContext with SQLite
        services.AddDbContext<TraceQDbContext>(options =>
            options.UseSqlite(connectionString ?? "Data Source=traceq.db"));

        // Register repositories
        services.AddScoped<IRequirementRepository, RequirementRepository>();
        services.AddScoped<IDashboardLayoutRepository, DashboardLayoutRepository>();

        // Register parsers
        services.AddScoped<ICsvParser, CsvParser>();

        // Register services
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IAuditService, AuditService>();

        return services;
    }

    /// <summary>
    /// Registers the Qdrant vector store and its configuration.
    /// The QdrantClient and QdrantVectorStore are registered as singletons
    /// because the gRPC client is thread-safe and the store manages a single connection.
    /// </summary>
    public static IServiceCollection AddQdrantVectorStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind QdrantOptions from configuration
        services.Configure<QdrantOptions>(
            configuration.GetSection(QdrantOptions.SectionName));

        // Register the Qdrant gRPC client as a singleton
        services.AddSingleton<IQdrantClient>(sp =>
        {
            var options = configuration.GetSection(QdrantOptions.SectionName).Get<QdrantOptions>()
                          ?? new QdrantOptions();

            return new QdrantClient(
                host: options.Host,
                port: options.GrpcPort);
        });

        // Register the vector store implementation
        services.AddSingleton<IVectorStore, QdrantVectorStore>();

        return services;
    }
}
