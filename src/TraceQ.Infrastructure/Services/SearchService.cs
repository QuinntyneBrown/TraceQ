using Microsoft.Extensions.Logging;
using TraceQ.Core.DTOs;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Services;

public class SearchService : ISearchService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IRequirementRepository _requirementRepository;
    private readonly ILogger<SearchService> _logger;

    public SearchService(
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IRequirementRepository requirementRepository,
        ILogger<SearchService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _requirementRepository = requirementRepository;
        _logger = logger;
    }

    public async Task<List<SearchResultDto>> SemanticSearchAsync(SearchRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            throw new ArgumentException("Search query cannot be empty.", nameof(request));
        }

        if (request.Top < 1 || request.Top > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Top must be between 1 and 100.");
        }

        // Generate embedding for the query text
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(request.Query);

        // Build filter dictionary from request.Filters (only include non-null filters)
        Dictionary<string, string>? filters = null;
        if (request.Filters != null)
        {
            filters = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(request.Filters.Type))
                filters["type"] = request.Filters.Type;
            if (!string.IsNullOrEmpty(request.Filters.State))
                filters["state"] = request.Filters.State;
            if (!string.IsNullOrEmpty(request.Filters.Priority))
                filters["priority"] = request.Filters.Priority;
            if (!string.IsNullOrEmpty(request.Filters.Module))
                filters["module"] = request.Filters.Module;
            if (!string.IsNullOrEmpty(request.Filters.Owner))
                filters["owner"] = request.Filters.Owner;

            if (filters.Count == 0)
                filters = null;
        }

        // Search vector store
        var vectorResults = await _vectorStore.SearchAsync(queryVector, request.Top, filters);

        if (vectorResults.Count == 0)
        {
            return new List<SearchResultDto>();
        }

        // Fetch full requirement details, preserving Qdrant's rank order
        var ids = vectorResults.Select(r => r.id).ToList();
        var requirements = await _requirementRepository.GetByIdsAsync(ids);
        var requirementMap = requirements.ToDictionary(r => r.Id);

        // Map to SearchResultDto list, preserving rank order from vector search
        var results = new List<SearchResultDto>();
        foreach (var (id, score) in vectorResults)
        {
            if (requirementMap.TryGetValue(id, out var requirement))
            {
                results.Add(new SearchResultDto
                {
                    Requirement = MapToDto(requirement),
                    SimilarityScore = score
                });
            }
        }

        return results;
    }

    public async Task<List<SearchResultDto>> FindSimilarAsync(Guid requirementId, int top = 10)
    {
        var requirement = await _requirementRepository.GetByIdAsync(requirementId);
        if (requirement == null)
        {
            throw new KeyNotFoundException($"Requirement with ID {requirementId} not found.");
        }

        // Re-embed the requirement text (Name + Description)
        var text = $"{requirement.Name} {requirement.Description ?? string.Empty}".Trim();
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(text);

        // Search for similar requirements (request one extra to account for self-match)
        var vectorResults = await _vectorStore.SearchAsync(queryVector, top + 1);

        if (vectorResults.Count == 0)
        {
            return new List<SearchResultDto>();
        }

        // Exclude the source requirement from results
        var filteredResults = vectorResults.Where(r => r.id != requirementId).Take(top).ToList();

        var ids = filteredResults.Select(r => r.id).ToList();
        var requirements = await _requirementRepository.GetByIdsAsync(ids);
        var requirementMap = requirements.ToDictionary(r => r.Id);

        var results = new List<SearchResultDto>();
        foreach (var (id, score) in filteredResults)
        {
            if (requirementMap.TryGetValue(id, out var req))
            {
                results.Add(new SearchResultDto
                {
                    Requirement = MapToDto(req),
                    SimilarityScore = score
                });
            }
        }

        return results;
    }

    public async Task<List<SimilarityClusterDto>> GetSimilarityClustersAsync(float threshold = 0.85f)
    {
        // Get all embedded requirements
        var allRequirements = (await _requirementRepository.GetAllAsync(pageSize: 10000)).Items;
        var embeddedRequirements = allRequirements.Where(r => r.IsEmbedded).ToList();

        if (embeddedRequirements.Count == 0)
        {
            return new List<SimilarityClusterDto>();
        }

        // Union-Find data structure for clustering
        var parent = new Dictionary<Guid, Guid>();
        var reqMap = embeddedRequirements.ToDictionary(r => r.Id);

        foreach (var req in embeddedRequirements)
        {
            parent[req.Id] = req.Id;
        }

        Guid Find(Guid x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]]; // path compression
                x = parent[x];
            }
            return x;
        }

        void Union(Guid a, Guid b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA != rootB)
            {
                parent[rootA] = rootB;
            }
        }

        // Track pairwise scores between similar requirements
        var pairwiseScores = new Dictionary<(Guid, Guid), float>();

        // For each requirement, find similar ones above threshold
        foreach (var req in embeddedRequirements)
        {
            try
            {
                var text = $"{req.Name} {req.Description ?? string.Empty}".Trim();
                var queryVector = await _embeddingService.GenerateEmbeddingAsync(text);
                var similar = await _vectorStore.SearchAsync(queryVector, 20);

                foreach (var (id, score) in similar)
                {
                    if (id != req.Id && score >= threshold && parent.ContainsKey(id))
                    {
                        Union(req.Id, id);

                        // Store pairwise scores (use sorted key to avoid duplicates)
                        var key = req.Id.CompareTo(id) < 0 ? (req.Id, id) : (id, req.Id);
                        pairwiseScores[key] = score;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to find similar requirements for {RequirementId}", req.Id);
            }
        }

        // Build clusters from union-find groups
        var clusterGroups = embeddedRequirements
            .GroupBy(r => Find(r.Id))
            .Where(g => g.Count() > 1) // Only include clusters with 2+ members
            .Take(50) // Limit to first 50 clusters
            .ToList();

        var clusters = new List<SimilarityClusterDto>();
        var clusterId = 1;

        foreach (var group in clusterGroups)
        {
            var members = new List<ClusterMemberDto>();
            var groupMembers = group.ToList();

            foreach (var member in groupMembers)
            {
                var clusterMember = new ClusterMemberDto
                {
                    Requirement = member,
                    PairwiseScores = new Dictionary<string, float>()
                };

                // Find pairwise scores with other members in the group
                foreach (var other in groupMembers)
                {
                    if (other.Id == member.Id) continue;

                    var key = member.Id.CompareTo(other.Id) < 0
                        ? (member.Id, other.Id)
                        : (other.Id, member.Id);

                    if (pairwiseScores.TryGetValue(key, out var score))
                    {
                        clusterMember.PairwiseScores[other.RequirementNumber] = score;
                    }
                }

                members.Add(clusterMember);
            }

            clusters.Add(new SimilarityClusterDto
            {
                ClusterId = clusterId++,
                Members = members
            });
        }

        return clusters;
    }

    private static RequirementDto MapToDto(Core.Models.Requirement r)
    {
        return new RequirementDto
        {
            Id = r.Id,
            RequirementNumber = r.RequirementNumber,
            Name = r.Name,
            Description = r.Description,
            Type = r.Type,
            State = r.State,
            Priority = r.Priority,
            Owner = r.Owner,
            CreatedDate = r.CreatedDate,
            ModifiedDate = r.ModifiedDate,
            Module = r.Module,
            ParentNumber = r.ParentNumber,
            TracedTo = string.IsNullOrEmpty(r.TracedTo)
                ? new List<string>()
                : r.TracedTo.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
            IsEmbedded = r.IsEmbedded
        };
    }
}
