using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraceQ.Cli.Services;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure;
using TraceQ.Infrastructure.Data;
using static TraceQ.Cli.ConsoleHelpers;

namespace TraceQ.Cli.Commands;

public static class ImportCommand
{
    public static Command Create()
    {
        var pathArgument = new Argument<FileInfo>("path", "Path to the CSV file to import");
        var dbOption = new Option<string>("--db", () => "traceq.db", "Path to the SQLite database file");

        var command = new Command("import", "Import a Windchill PLM CSV export into the TraceQ database")
        {
            pathArgument,
            dbOption
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var file = context.ParseResult.GetValueForArgument(pathArgument);
            var dbPath = context.ParseResult.GetValueForOption(dbOption)!;
            context.ExitCode = await ExecuteAsync(file, dbPath);
        });

        return command;
    }

    internal static async Task<int> ExecuteAsync(FileInfo file, string dbPath)
    {
        WriteHeader($"Importing: {file.Name}");

        // --- File-level checks ---
        WriteSectionHeader("File Checks");

        if (!file.Exists)
        {
            WriteError($"File not found: {file.FullName}");
            WriteVerdict(false, 1);
            return 1;
        }
        WritePass("File exists");

        if (!file.Name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            WriteError("File must have a .csv extension");
            WriteVerdict(false, 1);
            return 1;
        }
        WritePass("File has .csv extension");

        if (file.Length == 0)
        {
            WriteError("File is empty");
            WriteVerdict(false, 1);
            return 1;
        }
        WritePass($"File is not empty ({file.Length:N0} bytes)");

        // --- Build services with database ---
        var connectionString = $"Data Source={dbPath}";
        var services = ConfigureImportServices(connectionString);

        // Ensure database exists
        using (var scope = services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TraceQDbContext>();
            await context.Database.EnsureCreatedAsync();
        }

        WriteInfo($"Database: {dbPath}");

        // --- Import ---
        WriteSectionHeader("Import");

        using (var scope = services.CreateScope())
        {
            var importService = scope.ServiceProvider.GetRequiredService<IImportService>();

            await using var stream = file.OpenRead();
            var result = await importService.ImportAsync(stream, file.Name);

            Console.WriteLine();
            WriteSectionHeader("Results");

            if (result.InsertedCount > 0)
                WritePass($"Inserted:  {result.InsertedCount}");
            if (result.UpdatedCount > 0)
                WriteInfo($"Updated:   {result.UpdatedCount}");
            if (result.SkippedCount > 0)
                WriteInfo($"Skipped:   {result.SkippedCount}");
            if (result.ErrorCount > 0)
                WriteWarning($"Errors:    {result.ErrorCount}");

            var total = result.InsertedCount + result.UpdatedCount + result.SkippedCount + result.ErrorCount;
            WriteInfo($"Total:     {total}");

            Console.WriteLine();
            SetColor(ConsoleColor.Green);
            Console.WriteLine("  Import complete.");
            ResetColor();
            Console.WriteLine();
        }

        return 0;
    }

    internal static ServiceProvider ConfigureImportServices(string connectionString)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddConsole();
        });

        services.AddTraceQInfrastructure(connectionString);

        // No-op stubs for services that require ONNX/Qdrant
        services.AddSingleton<IEmbeddingService, NoOpEmbeddingService>();
        services.AddSingleton<IVectorStore, NoOpVectorStore>();

        return services.BuildServiceProvider();
    }
}
