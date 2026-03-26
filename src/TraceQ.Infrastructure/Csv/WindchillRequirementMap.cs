using CsvHelper.Configuration;
using TraceQ.Core.Models;

namespace TraceQ.Infrastructure.Csv;

public sealed class WindchillRequirementMap : ClassMap<Requirement>
{
    public WindchillRequirementMap()
    {
        Map(m => m.RequirementNumber).Name("Number");
        Map(m => m.Name).Name("Name");
        Map(m => m.Description).Name("Description").Optional();
        Map(m => m.Type).Name("Type").Optional();
        Map(m => m.State).Name("State").Optional();
        Map(m => m.Priority).Name("Priority").Optional();
        Map(m => m.Owner).Name("Owner").Optional();
        Map(m => m.CreatedDate).Name("Created On").Optional();
        Map(m => m.ModifiedDate).Name("Modified On").Optional();
        Map(m => m.Module).Name("Module").Optional();
        Map(m => m.ParentNumber).Name("Parent Number").Optional();
        Map(m => m.TracedTo).Name("Traced To").Optional();

        // Ignore properties that are not part of the CSV
        Map(m => m.Id).Ignore();
        Map(m => m.IsEmbedded).Ignore();
        Map(m => m.ImportedAt).Ignore();
        Map(m => m.ImportBatchId).Ignore();
        Map(m => m.ImportBatch).Ignore();
    }
}
