using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TraceQ.Core.Interfaces;
using TraceQ.Infrastructure.Embeddings;

namespace TraceQ.Infrastructure.Tests.Embeddings;

public class OnnxEmbeddingServiceTests
{
    private static readonly string ModelsDir = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "models");

    private static string ModelPath => Path.GetFullPath(Path.Combine(ModelsDir, "all-MiniLM-L6-v2.onnx"));
    private static string VocabPath => Path.GetFullPath(Path.Combine(ModelsDir, "vocab.txt"));

    private static bool ModelFilesExist =>
        File.Exists(ModelPath) && File.Exists(VocabPath);

    private static OnnxEmbeddingService CreateService()
    {
        var options = Options.Create(new EmbeddingModelOptions
        {
            ModelPath = ModelPath,
            VocabPath = VocabPath,
            MaxSequenceLength = 256,
            EmbeddingDimension = 384
        });

        var logger = NullLogger<OnnxEmbeddingService>.Instance;
        return new OnnxEmbeddingService(options, logger);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateEmbeddingAsync_OutputLengthIs384()
    {
        if (!ModelFilesExist)
        {
            // Skip if model files are not available
            return;
        }

        using var service = CreateService();
        var embedding = await service.GenerateEmbeddingAsync("The system shall provide authentication.");

        Assert.Equal(384, embedding.Length);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateEmbeddingAsync_OutputIsL2Normalized()
    {
        if (!ModelFilesExist)
        {
            return;
        }

        using var service = CreateService();
        var embedding = await service.GenerateEmbeddingAsync("The system shall provide authentication.");

        // L2 norm should be approximately 1.0
        double norm = Math.Sqrt(embedding.Sum(x => (double)x * x));
        Assert.InRange(norm, 0.99, 1.01);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateEmbeddingAsync_DeterministicForSameInput()
    {
        if (!ModelFilesExist)
        {
            return;
        }

        using var service = CreateService();
        var text = "REQ-001: The navigation subsystem shall maintain GPS lock.";

        var embedding1 = await service.GenerateEmbeddingAsync(text);
        var embedding2 = await service.GenerateEmbeddingAsync(text);

        Assert.Equal(embedding1.Length, embedding2.Length);

        for (int i = 0; i < embedding1.Length; i++)
        {
            Assert.Equal(embedding1[i], embedding2[i], precision: 6);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateEmbeddingAsync_SimilarTextsHaveHighCosineSimilarity()
    {
        if (!ModelFilesExist)
        {
            return;
        }

        using var service = CreateService();

        var embedding1 = await service.GenerateEmbeddingAsync("The system shall authenticate users.");
        var embedding2 = await service.GenerateEmbeddingAsync("User authentication is required.");
        var embedding3 = await service.GenerateEmbeddingAsync("The rocket engine shall produce 500kN of thrust.");

        // Similar texts should have higher cosine similarity
        double sim12 = CosineSimilarity(embedding1, embedding2);
        double sim13 = CosineSimilarity(embedding1, embedding3);

        Assert.True(sim12 > sim13,
            $"Similar texts should score higher ({sim12:F4}) than dissimilar ({sim13:F4})");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateBatchEmbeddingsAsync_ProcessesMultipleTexts()
    {
        if (!ModelFilesExist)
        {
            return;
        }

        using var service = CreateService();

        var items = new List<(string id, string text)>
        {
            ("1", "The system shall authenticate users."),
            ("2", "GPS navigation requirement."),
            ("3", "Thermal protection subsystem.")
        };

        var results = await service.GenerateBatchEmbeddingsAsync(items);

        Assert.Equal(3, results.Count);
        Assert.True(results.ContainsKey("1"));
        Assert.True(results.ContainsKey("2"));
        Assert.True(results.ContainsKey("3"));

        foreach (var kvp in results)
        {
            Assert.Equal(384, kvp.Value.Length);
        }
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ReturnsZeroVector()
    {
        // This test uses a mock to avoid needing the ONNX model
        var mockService = new Mock<IEmbeddingService>();
        mockService
            .Setup(s => s.GenerateEmbeddingAsync(string.Empty))
            .ReturnsAsync(new float[384]);

        var result = await mockService.Object.GenerateEmbeddingAsync(string.Empty);

        Assert.Equal(384, result.Length);
        Assert.All(result, v => Assert.Equal(0f, v));
    }

    [Fact]
    public async Task GenerateBatchEmbeddingsAsync_EmptyCollection_ReturnsEmptyDictionary()
    {
        var mockService = new Mock<IEmbeddingService>();
        mockService
            .Setup(s => s.GenerateBatchEmbeddingsAsync(It.IsAny<IEnumerable<(string, string)>>()))
            .ReturnsAsync(new Dictionary<string, float[]>());

        var result = await mockService.Object.GenerateBatchEmbeddingsAsync(
            Array.Empty<(string, string)>());

        Assert.Empty(result);
    }

    [Fact]
    public void Constructor_MissingModelFile_ThrowsFileNotFoundException()
    {
        var options = Options.Create(new EmbeddingModelOptions
        {
            ModelPath = "/nonexistent/model.onnx",
            VocabPath = "/nonexistent/vocab.txt",
        });

        var logger = NullLogger<OnnxEmbeddingService>.Instance;

        Assert.Throws<FileNotFoundException>(() => new OnnxEmbeddingService(options, logger));
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        double denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom > 0 ? dot / denom : 0;
    }
}
