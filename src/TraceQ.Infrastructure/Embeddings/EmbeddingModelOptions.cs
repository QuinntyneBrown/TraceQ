namespace TraceQ.Infrastructure.Embeddings;

public class EmbeddingModelOptions
{
    public const string SectionName = "EmbeddingModel";

    public string ModelPath { get; set; } = "./models/all-MiniLM-L6-v2.onnx";
    public string VocabPath { get; set; } = "./models/vocab.txt";
    public int MaxSequenceLength { get; set; } = 256;
    public int EmbeddingDimension { get; set; } = 384;
}
