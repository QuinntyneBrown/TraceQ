# Vector Embedding Generation - Detail Design

## 1. Overview

Vector embedding generation converts requirement text into 384-dimensional numerical vectors using a local ONNX model (all-MiniLM-L6-v2). This enables semantic similarity search by representing textual requirements as dense vectors in a shared embedding space, where semantically similar texts produce vectors with high cosine similarity.

The process involves:

1. **WordPiece tokenization** - Breaking input text into subword tokens using a 30,522-entry vocabulary
2. **BERT-style inference via ONNX Runtime** - Running the tokenized input through the all-MiniLM-L6-v2 transformer model
3. **Mean pooling with attention masking** - Aggregating per-token hidden states into a single sentence-level vector
4. **L2 normalization** - Producing a unit vector suitable for cosine similarity computation

Embeddings are generated through two paths:

- **Inline during CSV import** - Each requirement is embedded immediately as it is imported, ensuring vectors are available for search right away.
- **Background service polling** - An `EmbeddingBackgroundService` polls every 30 seconds for any requirements that lack embeddings (e.g., due to transient failures during import) and generates them as a safety net.

## 2. Components

### 2.1 OnnxEmbeddingService (Singleton)

The core embedding service responsible for converting text into vectors.

- **Lifecycle:** Registered as a Singleton. Loads the all-MiniLM-L6-v2.onnx model and vocab.txt at startup.
- **Thread Safety:** The `InferenceSession` is thread-safe and shared across all callers.
- **Methods:**
  - `GenerateEmbeddingAsync(string text)` - Tokenizes the input text, runs ONNX inference, applies mean pooling and L2 normalization, and returns a `float[384]` vector.
  - `GenerateBatchEmbeddingsAsync(IEnumerable<(string id, string text)>)` - Processes multiple texts sequentially and returns a `Dictionary<string, float[]>` mapping each id to its embedding vector.
- **Input Text Format:** `"{RequirementNumber} {Name} {Description}"` - The requirement number, name, and description are concatenated with spaces to form the input string for embedding.

### 2.2 WordPieceTokenizer

Handles subword tokenization compatible with the BERT/MiniLM vocabulary.

- **Vocabulary:** Loads `vocab.txt` containing 30,522 WordPiece tokens.
- **Max Sequence Length:** 256 tokens (including special tokens).
- **Special Tokens:**
  - `[CLS]` (token id 101) - Inserted at the start of every sequence.
  - `[SEP]` (token id 102) - Inserted at the end of every sequence.
  - `[PAD]` (token id 0) - Used to pad sequences shorter than 256 tokens.
- **Output - TokenizedInput:**
  - `input_ids` (`int[1, 256]`) - Token indices from the vocabulary.
  - `attention_mask` (`int[1, 256]`) - Binary mask where 1 indicates a real token and 0 indicates padding.
  - `token_type_ids` (`int[1, 256]`) - All zeros (single-segment input).
- **Processing Steps:**
  1. Case folding (lowercase conversion).
  2. Punctuation splitting (separate punctuation from words).
  3. WordPiece subword splitting (greedily match longest prefix in vocabulary).
  4. Unknown token `[UNK]` (token id 100) fallback for unrecognized subwords.

### 2.3 ONNX Inference Pipeline

The inference pipeline connects tokenization to vector output:

1. **Create Inputs:** Build `NamedOnnxValue` tensors for `input_ids`, `attention_mask`, and `token_type_ids`, each with shape `[1, seqLen]`.
2. **Run Inference:** Call `InferenceSession.Run()` with the input tensors. The model outputs `last_hidden_state` with shape `[1, seqLen, 384]` - one 384-dimensional hidden state per token.
3. **Mean Pooling:** Sum token embeddings weighted by the `attention_mask` (so padding tokens contribute zero), then divide by the sum of the mask values. This produces a single `float[384]` sentence embedding.
4. **L2 Normalization:** Divide each dimension by the vector magnitude (`sqrt(sum of squares)`). The result is a unit vector where dot product equals cosine similarity.

### 2.4 EmbeddingBackgroundService (IHostedService)

A hosted background service that ensures all requirements eventually receive embeddings.

- **Polling Interval:** Every 30 seconds via a `Timer`.
- **Execution Flow:**
  1. Fetches unembedded requirements from `IRequirementRepository.GetUnembeddedAsync()`.
  2. Generates embeddings via `IEmbeddingService.GenerateBatchEmbeddingsAsync()`.
  3. Upserts vectors to Qdrant via `IVectorStore.UpsertBatchAsync()` (chunked at 100 vectors per batch).
  4. Marks requirements as embedded via `IRequirementRepository.MarkAsEmbeddedAsync()`.
- **Purpose:** Acts as a safety net for requirements that failed inline embedding during CSV import (e.g., due to transient errors or resource contention).

### 2.5 QdrantVectorStore

Manages vector storage and retrieval via the Qdrant vector database.

- **Methods:**
  - `UpsertAsync` - Inserts or updates a single vector with a `Guid` id, `float[384]` vector, and a payload dictionary.
  - `UpsertBatchAsync` - Batch upsert operation, chunked at `MaxBatchSize = 100` to avoid oversized gRPC messages.
- **Payload Fields:** Each vector stores metadata including `number`, `name`, `type`, `state`, and `module`.
- **Collection Configuration:** 384 dimensions with cosine distance metric.

### 2.6 Configuration (appsettings.json)

```json
{
  "EmbeddingModel": {
    "ModelPath": "./models/all-MiniLM-L6-v2.onnx",
    "VocabPath": "./models/vocab.txt",
    "MaxSequenceLength": 256,
    "EmbeddingDimension": 384
  }
}
```

| Setting | Value | Description |
|---------|-------|-------------|
| `EmbeddingModel.ModelPath` | `./models/all-MiniLM-L6-v2.onnx` | Path to the ONNX model file |
| `EmbeddingModel.VocabPath` | `./models/vocab.txt` | Path to the WordPiece vocabulary file |
| `EmbeddingModel.MaxSequenceLength` | `256` | Maximum number of tokens per input sequence |
| `EmbeddingModel.EmbeddingDimension` | `384` | Dimensionality of the output embedding vectors |

## 3. Class Diagram

![Class Diagram](class-diagram.puml.png)

## 4. Sequence Diagram

![Sequence Diagram](sequence-diagram.puml.png)

## 5. C4 Diagram

![C4 Diagram](c4-vector-embedding.drawio.png)
