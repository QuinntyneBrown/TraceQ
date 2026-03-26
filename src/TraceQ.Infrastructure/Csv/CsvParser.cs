using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using TraceQ.Core.Interfaces;
using TraceQ.Core.Models;

namespace TraceQ.Infrastructure.Csv;

public class CsvParser : ICsvParser
{
    public async Task<List<RequirementParseResult>> ParseAsync(Stream csvStream)
    {
        var results = new List<RequirementParseResult>();

        using var reader = new StreamReader(csvStream, detectEncodingFromByteOrderMarks: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
        });

        csv.Context.RegisterClassMap<WindchillRequirementMap>();

        // Read header first
        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1;
        while (await csv.ReadAsync())
        {
            rowNumber++;
            try
            {
                var requirement = csv.GetRecord<Requirement>();

                if (requirement == null || string.IsNullOrWhiteSpace(requirement.RequirementNumber))
                {
                    results.Add(new RequirementParseResult
                    {
                        Success = false,
                        ErrorMessage = $"Row {rowNumber}: Missing required 'Number' field.",
                        RequirementNumber = string.Empty
                    });
                    continue;
                }

                requirement.Id = Guid.NewGuid();
                requirement.ImportedAt = DateTime.UtcNow;

                results.Add(new RequirementParseResult
                {
                    Success = true,
                    Requirement = requirement,
                    RequirementNumber = requirement.RequirementNumber
                });
            }
            catch (Exception ex)
            {
                results.Add(new RequirementParseResult
                {
                    Success = false,
                    ErrorMessage = $"Row {rowNumber}: {ex.Message}",
                    RequirementNumber = string.Empty
                });
            }
        }

        return results;
    }
}
