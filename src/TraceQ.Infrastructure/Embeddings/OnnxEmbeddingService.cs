using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using TraceQ.Core.Interfaces;

namespace TraceQ.Infrastructure.Embeddings;

/// <summary>
/// Embedding service using a local ONNX model (all-MiniLM-L6-v2) for air-gapped deployment.
/// Registered as a singleton — the InferenceSession is thread-safe for concurrent reads.
/// </summary>
public class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly WordPieceTokenizer _tokenizer;
    private readonly EmbeddingModelOptions _options;
    private readonly ILogger<OnnxEmbeddingService> _logger;
    private bool _disposed;

    public OnnxEmbeddingService(
        IOptions<EmbeddingModelOptions> options,
        ILogger<OnnxEmbeddingService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var modelPath = _options.ModelPath;
        var vocabPath = _options.VocabPath;

        if (!File.Exists(modelPath))
            throw new FileNotFoundException(
                $"ONNX model file not found at '{modelPath}'. Run scripts/download-model.ps1 to download it.",
                modelPath);

        if (!File.Exists(vocabPath))
            throw new FileNotFoundException(
                $"Vocabulary file not found at '{vocabPath}'. Run scripts/download-model.ps1 to download it.",
                vocabPath);

        _logger.LogInformation("Loading ONNX model from {ModelPath}", modelPath);
        var sessionOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(modelPath, sessionOptions);

        _logger.LogInformation("Loading vocabulary from {VocabPath}", vocabPath);
        _tokenizer = WordPieceTokenizer.FromVocabFile(vocabPath, _options.MaxSequenceLength);

        _logger.LogInformation("ONNX embedding service initialized (dimension={Dimension}, maxSeq={MaxSeq})",
            _options.EmbeddingDimension, _options.MaxSequenceLength);
    }

    /// <summary>
    /// Constructor for unit testing with pre-built session and tokenizer.
    /// </summary>
    internal OnnxEmbeddingService(
        InferenceSession session,
        WordPieceTokenizer tokenizer,
        EmbeddingModelOptions options,
        ILogger<OnnxEmbeddingService> logger)
    {
        _session = session;
        _tokenizer = tokenizer;
        _options = options;
        _logger = logger;
    }

    public bool IsAvailable => true;

    /// <inheritdoc />
    public Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(new float[_options.EmbeddingDimension]);
        }

        var embedding = GenerateEmbedding(text);
        return Task.FromResult(embedding);
    }

    /// <inheritdoc />
    public Task<Dictionary<string, float[]>> GenerateBatchEmbeddingsAsync(
        IEnumerable<(string id, string text)> items)
    {
        var results = new Dictionary<string, float[]>();

        foreach (var (id, text) in items)
        {
            var embedding = string.IsNullOrWhiteSpace(text)
                ? new float[_options.EmbeddingDimension]
                : GenerateEmbedding(text);

            results[id] = embedding;
        }

        return Task.FromResult(results);
    }

    /// <summary>
    /// Generates an embedding vector for the given text using the ONNX model.
    /// Uses mean pooling of token embeddings (masked by attention mask) to match
    /// sentence-transformers output.
    /// </summary>
    private float[] GenerateEmbedding(string text)
    {
        var maxLen = _options.MaxSequenceLength;

        // Tokenize
        var (inputIds, attentionMask, tokenTypeIds) = _tokenizer.Tokenize(text, maxLen);

        // Create input tensors with shape [1, seqLen]
        var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, maxLen });
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, maxLen });
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, maxLen });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);

        // Extract last_hidden_state: shape [1, seqLen, hiddenSize]
        var outputTensor = results.First().AsTensor<float>();
        var shape = outputTensor.Dimensions.ToArray();
        int seqLen = shape[1];
        int hiddenSize = shape[2];

        // Mean pooling: average of token embeddings weighted by attention mask
        var pooled = MeanPool(outputTensor, attentionMask, seqLen, hiddenSize);

        // L2-normalize
        L2Normalize(pooled);

        return pooled;
    }

    /// <summary>
    /// Performs mean pooling: averages token embeddings, excluding padding tokens.
    /// This matches how sentence-transformers produces embeddings.
    /// </summary>
    private static float[] MeanPool(Tensor<float> lastHiddenState, long[] attentionMask, int seqLen, int hiddenSize)
    {
        var result = new float[hiddenSize];
        float maskSum = 0;

        for (int t = 0; t < seqLen; t++)
        {
            if (attentionMask[t] == 0)
                continue;

            maskSum += 1.0f;
            for (int h = 0; h < hiddenSize; h++)
            {
                result[h] += lastHiddenState[0, t, h];
            }
        }

        // Avoid division by zero
        if (maskSum > 0)
        {
            for (int h = 0; h < hiddenSize; h++)
            {
                result[h] /= maskSum;
            }
        }

        return result;
    }

    /// <summary>
    /// L2-normalizes a vector in-place so that its Euclidean length is 1.
    /// </summary>
    private static void L2Normalize(float[] vector)
    {
        double sumOfSquares = 0;
        for (int i = 0; i < vector.Length; i++)
        {
            sumOfSquares += (double)vector[i] * vector[i];
        }

        float norm = (float)Math.Sqrt(sumOfSquares);

        if (norm > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= norm;
            }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
