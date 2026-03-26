using System.CommandLine;
using System.CommandLine.Invocation;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraceQ.Core.Interfaces;

namespace TraceQ.Cli.Commands;

public static class ValidateCommand
{
    private static readonly string[] RequiredColumns = ["Number", "Name"];

    private static readonly string[] KnownColumns =
    [
        "Number", "Name", "Description", "Type", "State", "Priority",
        "Owner", "Created On", "Modified On", "Module", "Parent Number", "Traced To"
    ];

    public static Command Create(IServiceProvider services)
    {
        var pathArgument = new Argument<FileInfo>("path", "Path to the CSV file to validate");
        var command = new Command("validate", "Validate a Windchill PLM CSV export for importability")
        {
            pathArgument
        };

        command.SetHandler(async (InvocationContext context) =>
        {
            var file = context.ParseResult.GetValueForArgument(pathArgument);
            context.ExitCode = await ExecuteAsync(file, services);
        });

        return command;
    }

    internal static async Task<int> ExecuteAsync(FileInfo file, IServiceProvider services)
    {
        var hasErrors = false;
        var warnings = new List<string>();

        WriteHeader($"Validating: {file.Name}");

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

        // --- Structure-level checks (header) ---
        WriteSectionHeader("Structure Checks");

        string[] headerColumns;
        try
        {
            headerColumns = await ReadHeaderColumnsAsync(file.FullName);
        }
        catch (Exception ex)
        {
            WriteError($"Failed to read CSV header: {ex.Message}");
            WriteVerdict(false, 1);
            return 1;
        }

        if (headerColumns.Length == 0)
        {
            WriteError("CSV has no header columns");
            WriteVerdict(false, 1);
            return 1;
        }

        WriteInfo($"Columns found: {string.Join(", ", headerColumns)}");

        // Check required columns
        var missingRequiredCount = 0;
        var normalizedHeaders = headerColumns.Select(h => h.Trim().ToLowerInvariant()).ToArray();
        foreach (var required in RequiredColumns)
        {
            if (normalizedHeaders.Contains(required.ToLowerInvariant()))
            {
                WritePass($"Required column '{required}' present");
            }
            else
            {
                WriteError($"Required column '{required}' is missing");
                hasErrors = true;
                missingRequiredCount++;
            }
        }

        // Check for known optional columns
        var knownLower = KnownColumns.Select(c => c.ToLowerInvariant()).ToHashSet();
        var unknownColumns = headerColumns
            .Where(h => !knownLower.Contains(h.Trim().ToLowerInvariant()))
            .ToList();

        if (unknownColumns.Count > 0)
        {
            foreach (var col in unknownColumns)
            {
                WriteWarning($"Unknown column '{col}' will be ignored during import");
                warnings.Add($"Unknown column: {col}");
            }
        }

        if (hasErrors)
        {
            WriteVerdict(false, missingRequiredCount);
            return 1;
        }

        // --- Row-level validation ---
        WriteSectionHeader("Row Validation");

        using var scope = services.CreateScope();
        var parser = scope.ServiceProvider.GetRequiredService<ICsvParser>();

        List<RequirementParseResult> parseResults;
        try
        {
            await using var stream = file.OpenRead();
            parseResults = await parser.ParseAsync(stream);
        }
        catch (Exception ex)
        {
            WriteError($"Failed to parse CSV: {ex.Message}");
            WriteVerdict(false, 0);
            return 1;
        }

        var totalRows = parseResults.Count;
        var validRows = parseResults.Count(r => r.Success);
        var errorRows = parseResults.Count(r => !r.Success);

        WriteInfo($"Total rows:  {totalRows}");
        if (validRows > 0) WritePass($"Valid rows:  {validRows}");
        if (errorRows > 0) WriteError($"Error rows:  {errorRows}");

        // Per-row errors
        var errors = parseResults.Where(r => !r.Success).ToList();
        if (errors.Count > 0)
        {
            hasErrors = true;
            Console.WriteLine();
            WriteSectionHeader("Row Errors");
            foreach (var error in errors)
            {
                WriteError(error.ErrorMessage ?? "Unknown error");
            }
        }

        // --- Warnings ---
        if (warnings.Count > 0)
        {
            Console.WriteLine();
            WriteSectionHeader("Warnings");
            foreach (var warning in warnings)
            {
                WriteWarning(warning);
            }
        }

        // --- Verdict ---
        Console.WriteLine();
        if (hasErrors)
        {
            var errorCount = errorRows + (parseResults.Count == 0 ? 0 : 0);
            WriteVerdict(false, errorRows);
            return 1;
        }

        WriteVerdict(true, 0);
        return 0;
    }

    private static async Task<string[]> ReadHeaderColumnsAsync(string filePath)
    {
        using var reader = new StreamReader(filePath, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        });

        await csv.ReadAsync();
        csv.ReadHeader();

        return csv.HeaderRecord ?? [];
    }

    // --- Console output helpers ---

    private static void WriteHeader(string message)
    {
        Console.WriteLine();
        SetColor(ConsoleColor.Cyan);
        Console.WriteLine($"  {message}");
        ResetColor();
        Console.WriteLine(new string('─', Math.Min(60, message.Length + 4)));
        Console.WriteLine();
    }

    private static void WriteSectionHeader(string title)
    {
        SetColor(ConsoleColor.White);
        Console.WriteLine($"  [{title}]");
        ResetColor();
    }

    private static void WritePass(string message)
    {
        SetColor(ConsoleColor.Green);
        Console.Write("    PASS  ");
        ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteError(string message)
    {
        SetColor(ConsoleColor.Red);
        Console.Write("    FAIL  ");
        ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteWarning(string message)
    {
        SetColor(ConsoleColor.Yellow);
        Console.Write("    WARN  ");
        ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteInfo(string message)
    {
        SetColor(ConsoleColor.Gray);
        Console.Write("    INFO  ");
        ResetColor();
        Console.WriteLine(message);
    }

    private static void WriteVerdict(bool passed, int errorCount)
    {
        Console.WriteLine();
        if (passed)
        {
            SetColor(ConsoleColor.Green);
            Console.WriteLine("  Result: PASS — file can be imported");
        }
        else
        {
            SetColor(ConsoleColor.Red);
            Console.WriteLine($"  Result: FAIL — {errorCount} error(s) found");
        }
        ResetColor();
        Console.WriteLine();
    }

    private static void SetColor(ConsoleColor color)
    {
        try { Console.ForegroundColor = color; } catch { /* redirected output */ }
    }

    private static void ResetColor()
    {
        try { Console.ResetColor(); } catch { /* redirected output */ }
    }
}
