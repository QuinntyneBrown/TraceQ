using Microsoft.EntityFrameworkCore;
using Serilog;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Services;

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

    // Register TraceQ Infrastructure services (DbContext, repositories, parsers, etc.)
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=traceq.db";
    builder.Services.AddTraceQInfrastructure(connectionString);

    // Register placeholder services for IEmbeddingService and IVectorStore
    // These will be replaced by real implementations in later sprints
    builder.Services.AddSingleton<IEmbeddingService, NoOpEmbeddingService>();
    builder.Services.AddSingleton<IVectorStore, NoOpVectorStore>();

    var app = builder.Build();

    // Ensure database is created
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TraceQDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

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
