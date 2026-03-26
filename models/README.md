# Embedding Model Setup

TraceQ uses the **all-MiniLM-L6-v2** sentence-transformer model in ONNX format for generating requirement embeddings locally (air-gapped, no external API calls).

## Required Files

Place the following files in this `models/` directory:

1. **`all-MiniLM-L6-v2.onnx`** — The ONNX-exported model
2. **`vocab.txt`** — The WordPiece tokenizer vocabulary

## Download Instructions

### Option A: From Hugging Face (internet-connected machine)

1. Visit https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2
2. Download the ONNX model from the `onnx/` directory in the repository files.

   Alternatively, use the Hugging Face CLI:

   ```bash
   pip install huggingface-hub
   huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 onnx/model.onnx --local-dir .
   ```

3. Rename `onnx/model.onnx` to `all-MiniLM-L6-v2.onnx` and place it here.
4. Download `vocab.txt` from the root of the same repository and place it here.

### Option B: Export from PyTorch (internet-connected machine)

```bash
pip install optimum[exporters] sentence-transformers
optimum-cli export onnx --model sentence-transformers/all-MiniLM-L6-v2 ./minilm-onnx/
```

Copy `./minilm-onnx/model.onnx` to this directory as `all-MiniLM-L6-v2.onnx`.
Copy `vocab.txt` from the model directory here.

### Option C: Air-gapped transfer

On a connected machine, download the files using either method above, then transfer
them to the air-gapped environment via approved media (USB, optical disc, etc.)
following your organization's data transfer procedures.

## Model Details

| Property             | Value                  |
|----------------------|------------------------|
| Embedding Dimension  | 384                    |
| Max Sequence Length   | 256 tokens             |
| Model Size           | ~80 MB (ONNX)         |
| Vocabulary Size      | 30,522 tokens          |
| Runtime              | ONNX Runtime (CPU)     |

## Verification

After placing the files, verify the directory contains:

```
models/
  all-MiniLM-L6-v2.onnx
  vocab.txt
  README.md
```

The application will load these files on startup from the paths configured in
`appsettings.json` under `EmbeddingModel:ModelPath` and `EmbeddingModel:VocabPath`.
