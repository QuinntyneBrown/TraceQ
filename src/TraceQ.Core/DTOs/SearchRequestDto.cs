namespace TraceQ.Core.DTOs;

public class SearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int Top { get; set; } = 20;
    public SearchFiltersDto? Filters { get; set; }
}
