using Serilog;
using TraceQ.Core.Interfaces;

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

    // Configure Kestrel for localhost:5000 only
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenLocalhost(5000);
    });

    // Configure CORS for localhost:4200 only
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    // Add controllers
    builder.Services.AddControllers();

    // Register application services
    // ICsvParser — implemented in TraceQ.Infrastructure
    // IImportService — implemented in TraceQ.Infrastructure
    // IEmbeddingService — implemented in TraceQ.Infrastructure
    // IVectorStore — implemented in TraceQ.Infrastructure
    // IRequirementRepository — implemented in TraceQ.Infrastructure
    // IDashboardLayoutRepository — implemented in TraceQ.Infrastructure
    // ISearchService — implemented in TraceQ.Infrastructure
    //
    // Service registrations will be added here once implementations are created.
    // Example:
    // builder.Services.AddScoped<ICsvParser, CsvParser>();
    // builder.Services.AddScoped<IImportService, ImportService>();
    // builder.Services.AddScoped<IEmbeddingService, OnnxEmbeddingService>();
    // builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
    // builder.Services.AddScoped<IRequirementRepository, RequirementRepository>();
    // builder.Services.AddScoped<IDashboardLayoutRepository, DashboardLayoutRepository>();
    // builder.Services.AddScoped<ISearchService, SearchService>();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.MapControllers();

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
