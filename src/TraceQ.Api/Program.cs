using Microsoft.EntityFrameworkCore;
using Serilog;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Embeddings;
using TraceQ.Infrastructure.Health;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("./logs/traceq-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TraceQ API");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("./logs/traceq-.log", rollingInterval: RollingInterval.Day));

    // -------------------------------------------------------------------------
    // L2-7.1: Localhost-Only Binding
    // TraceQ is designed for air-gapped environments. Kestrel is explicitly
    // configured to bind only to the loopback interface (127.0.0.1:5000).
    // This prevents any network-accessible exposure of the API.
    // The "Urls" key in appsettings.json also enforces http://localhost:5000.
    // -------------------------------------------------------------------------
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5000);
    });

    // -------------------------------------------------------------------------
    // L2-7.4: CORS Configuration
    // Only the local Angular frontend at http://localhost:4200 is permitted.
    // AllowAnyOrigin() is NOT used. Methods and headers are explicitly listed.
    // -------------------------------------------------------------------------
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .WithMethods("GET", "POST", "PUT", "DELETE")
                .WithHeaders("Content-Type", "Authorization");
        });
    });

    // Add controllers
    builder.Services.AddControllers();

    // Register TraceQ Infrastructure services (DbContext, repositories, parsers, etc.)
    var connectionString =
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? builder.Configuration.GetConnectionString("Sqlite")
        ?? "Data Source=traceq.db";
    builder.Services.AddTraceQInfrastructure(connectionString);

    // Register local ONNX embedding service and background embedding worker.
    // Startup fails if the production model files cannot be loaded.
    builder.Services.AddEmbeddingServices(builder.Configuration);

    // Register Qdrant vector store (localhost-only). Startup fails if the
    // vector store cannot connect and initialize the configured collection.
    builder.Services.AddQdrantVectorStore(builder.Configuration);

    // -------------------------------------------------------------------------
    // L2-7.2: Network Egress Verification
    // All NuGet packages used in TraceQ have been audited:
    //   - CsvHelper: File-based CSV parsing, no network access required.
    //   - Microsoft.EntityFrameworkCore.Sqlite: Local SQLite database, no network.
    //   - Microsoft.ML.OnnxRuntime: Local ONNX model inference, no network.
    //   - Qdrant.Client: Connects to localhost-only Qdrant instance.
    //   - Serilog: Writes to console and local files only.
    //   - No HttpClient is configured to reach external URLs.
    // The AirGapHealthCheck verifies at startup that the ONNX model and vocab
    // files exist locally, and that Qdrant is reachable on localhost.
    // -------------------------------------------------------------------------

    // Register health checks for air-gap compliance verification
    builder.Services.AddHealthChecks()
        .AddCheck<AirGapHealthCheck>("air-gap");

    var app = builder.Build();

    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TraceQDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    // Force production dependencies to initialize before the app starts serving.
    _ = app.Services.GetRequiredService<IEmbeddingService>();

    // Initialize the vector store (connects to Qdrant, creates collection if needed).
    var vectorStore = app.Services.GetRequiredService<IVectorStore>();
    await vectorStore.InitializeAsync();

    // Configure the HTTP request pipeline
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapControllers();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
