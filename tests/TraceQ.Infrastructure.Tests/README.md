# TraceQ.Infrastructure.Tests

Unit and integration tests for the TraceQ Infrastructure layer. Covers CSV parsing, embedding generation, tokenization, vector store operations, search logic, and audit logging.

## Running

```bash
dotnet test
```

To skip integration tests that require external dependencies (ONNX model files, running Qdrant instance):

```bash
dotnet test --filter "Category!=Integration"
```

## Test classes

| Class | File | Coverage |
|-------|------|----------|
| `CsvParserTests` | Root | Full/minimal columns, quoted fields, case-insensitive headers, missing fields, UTF-8 BOM, extra columns |
| `WordPieceTokenizerTests` | Embeddings/ | CLS/SEP tokens, attention masks, truncation, padding, subword splitting, Unicode, punctuation |
| `OnnxEmbeddingServiceTests` | Embeddings/ | 384-dim output, L2 normalization, determinism, cosine similarity, batch processing. **Integration** — requires model files |
| `SearchServiceTests` | Services/ | Semantic search with filters, empty query validation, top-N range, rank order, find-similar with self-exclusion |
| `AuditServiceTests` | Services/ | Import/search/deletion event logging, pagination, event type filtering. Uses SQLite in-memory |
| `QdrantVectorStoreTests` | VectorStore/ | Collection init, batch chunking, upsert operations. **Integration** tests for round-trip search and filtered queries |

## Integration tests

Tests marked with `[Trait("Category", "Integration")]` require:

- **ONNX model files** in `./models/` (`all-MiniLM-L6-v2.onnx`, `vocab.txt`) for embedding tests
- **Qdrant running** on `localhost:6334` for vector store tests

## Dependencies

- xUnit 2.5.3
- Moq 4.20.72
- FluentAssertions 8.9.0
- Microsoft.EntityFrameworkCore (SQLite in-memory for service tests)
