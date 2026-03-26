using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TraceQ.Cli.Commands;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure.Csv;

namespace TraceQ.Cli;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var services = ConfigureServices();

        var rootCommand = new RootCommand("TraceQ CLI — requirements intelligence tool");
        rootCommand.AddCommand(ValidateCommand.Create(services));

        return await rootCommand.InvokeAsync(args);
    }

    internal static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Warning);
            builder.AddConsole();
        });

        services.AddScoped<ICsvParser, CsvParser>();

        return services.BuildServiceProvider();
    }
}
