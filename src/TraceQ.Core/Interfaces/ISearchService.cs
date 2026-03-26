using TraceQ.Core.DTOs;

namespace TraceQ.Core.Interfaces;

public interface ISearchService
{
    Task<List<SearchResultDto>> SemanticSearchAsync(SearchRequestDto request);
    Task<List<SearchResultDto>> FindSimilarAsync(Guid requirementId, int top = 10);
    Task<List<SimilarityClusterDto>> GetSimilarityClustersAsync(float threshold = 0.85f);
}
