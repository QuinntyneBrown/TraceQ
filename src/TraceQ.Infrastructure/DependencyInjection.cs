using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure.Csv;
using TraceQ.Infrastructure.Data;
using TraceQ.Infrastructure.Services;

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

        // Register parsers
        services.AddScoped<ICsvParser, CsvParser>();

        // Register services
        services.AddScoped<IImportService, ImportService>();

        return services;
    }
}
