namespace TraceQ.Core.DTOs;

public class FacetsDto
{
    public List<FacetValueDto> Types { get; set; } = new();
    public List<FacetValueDto> States { get; set; } = new();
    public List<FacetValueDto> Priorities { get; set; } = new();
    public List<FacetValueDto> Modules { get; set; } = new();
    public List<FacetValueDto> Owners { get; set; } = new();
}
