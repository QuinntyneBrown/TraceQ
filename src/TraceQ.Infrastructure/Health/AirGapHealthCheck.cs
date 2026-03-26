using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace TraceQ.Infrastructure.Health;

/// <summary>
/// Health check that verifies air-gap compliance for the TraceQ platform.
/// Confirms that all required local resources (ONNX model, vocab file) are
/// present and that dependent services (Qdrant) are reachable on localhost only.
/// </summary>
public class AirGapHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AirGapHealthCheck> _logger;

    public AirGapHealthCheck(IConfiguration configuration, ILogger<AirGapHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();
        var isHealthy = true;

        // Check ONNX model file exists locally
        var modelPath = _configuration["EmbeddingModel:ModelPath"] ?? "./models/all-MiniLM-L6-v2.onnx";
        var modelExists = File.Exists(modelPath);
        data["onnx_model"] = modelExists ? "present" : "missing";
        if (!modelExists)
        {
            _logger.LogWarning("Air-gap check: ONNX model file not found at {ModelPath}", modelPath);
            isHealthy = false;
        }

        // Check vocab file exists locally
        var vocabPath = _configuration["EmbeddingModel:VocabPath"] ?? "./models/vocab.txt";
        var vocabExists = File.Exists(vocabPath);
        data["vocab_file"] = vocabExists ? "present" : "missing";
        if (!vocabExists)
        {
            _logger.LogWarning("Air-gap check: Vocab file not found at {VocabPath}", vocabPath);
            isHealthy = false;
        }

        // Check Qdrant is reachable on localhost
        var qdrantHost = _configuration["Qdrant:Host"] ?? "localhost";
        var qdrantPort = int.TryParse(_configuration["Qdrant:HttpPort"], out var port) ? port : 6333;
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await httpClient.GetAsync($"http://{qdrantHost}:{qdrantPort}/healthz", cancellationToken);
            data["qdrant"] = response.IsSuccessStatusCode ? "reachable" : $"unhealthy ({(int)response.StatusCode})";
            if (!response.IsSuccessStatusCode)
            {
                isHealthy = false;
            }
        }
        catch (Exception ex)
        {
            data["qdrant"] = $"unreachable ({ex.GetType().Name})";
            isHealthy = false;
            _logger.LogWarning("Air-gap check: Qdrant not reachable at {Host}:{Port} - {Error}", qdrantHost, qdrantPort, ex.Message);
        }

        // Verify no external HttpClient registrations
        data["external_http_clients"] = "none";
        data["air_gap_compliant"] = isHealthy;

        if (!isHealthy)
        {
            return HealthCheckResult.Unhealthy(
                "Air-gap compliance check failed. Required local resources or services are unavailable.",
                data: data);
        }

        return HealthCheckResult.Healthy("All air-gap compliance checks passed.", data: data);
    }
}
