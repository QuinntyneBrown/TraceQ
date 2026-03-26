using System.Globalization;
using System.Text;

namespace TraceQ.Infrastructure.Embeddings;

/// <summary>
/// WordPiece tokenizer compatible with BERT/sentence-transformers models.
/// Loads vocabulary from a vocab.txt file and tokenizes text into sub-word tokens.
/// </summary>
public class WordPieceTokenizer
{
    private readonly Dictionary<string, int> _vocab;
    private readonly int _clsTokenId;
    private readonly int _sepTokenId;
    private readonly int _unkTokenId;
    private readonly int _padTokenId;
    private readonly int _maxSequenceLength;

    /// <summary>
    /// Creates a WordPieceTokenizer from a dictionary of token-to-id mappings.
    /// </summary>
    public WordPieceTokenizer(Dictionary<string, int> vocab, int maxSequenceLength = 256)
    {
        _vocab = vocab ?? throw new ArgumentNullException(nameof(vocab));
        _maxSequenceLength = maxSequenceLength;

        _clsTokenId = _vocab.GetValueOrDefault("[CLS]", 101);
        _sepTokenId = _vocab.GetValueOrDefault("[SEP]", 102);
        _unkTokenId = _vocab.GetValueOrDefault("[UNK]", 100);
        _padTokenId = _vocab.GetValueOrDefault("[PAD]", 0);
    }

    /// <summary>
    /// Loads a WordPieceTokenizer from a vocab.txt file on disk.
    /// Each line contains one token; line number (0-based) is the token ID.
    /// </summary>
    public static WordPieceTokenizer FromVocabFile(string vocabPath, int maxSequenceLength = 256)
    {
        if (!File.Exists(vocabPath))
            throw new FileNotFoundException($"Vocabulary file not found: {vocabPath}", vocabPath);

        var vocab = new Dictionary<string, int>();
        var lines = File.ReadAllLines(vocabPath, Encoding.UTF8);

        for (int i = 0; i < lines.Length; i++)
        {
            var token = lines[i].Trim();
            if (!string.IsNullOrEmpty(token) && !vocab.ContainsKey(token))
            {
                vocab[token] = i;
            }
        }

        return new WordPieceTokenizer(vocab, maxSequenceLength);
    }

    /// <summary>
    /// Tokenizes text into input tensors for BERT-style models.
    /// Returns input_ids, attention_mask, and token_type_ids arrays of length maxLength.
    /// </summary>
    public (long[] InputIds, long[] AttentionMask, long[] TokenTypeIds) Tokenize(string text, int maxLength = 0)
    {
        if (maxLength <= 0)
            maxLength = _maxSequenceLength;

        var tokenIds = new List<int>();

        // Add [CLS] token
        tokenIds.Add(_clsTokenId);

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Preprocess text
            var preprocessed = Preprocess(text);

            // Split into words
            var words = SplitOnWhitespaceAndPunctuation(preprocessed);

            // WordPiece tokenize each word
            foreach (var word in words)
            {
                var subTokens = WordPieceTokenize(word);
                foreach (var subToken in subTokens)
                {
                    tokenIds.Add(subToken);

                    // Reserve space for [SEP] token
                    if (tokenIds.Count >= maxLength - 1)
                        break;
                }

                if (tokenIds.Count >= maxLength - 1)
                    break;
            }
        }

        // Add [SEP] token
        tokenIds.Add(_sepTokenId);

        // Truncate if needed (should already be within bounds, but safety check)
        if (tokenIds.Count > maxLength)
        {
            tokenIds = tokenIds.Take(maxLength - 1).ToList();
            tokenIds.Add(_sepTokenId);
        }

        int realTokenCount = tokenIds.Count;

        // Create output arrays
        var inputIds = new long[maxLength];
        var attentionMask = new long[maxLength];
        var tokenTypeIds = new long[maxLength]; // All zeros for single-sentence input

        for (int i = 0; i < realTokenCount; i++)
        {
            inputIds[i] = tokenIds[i];
            attentionMask[i] = 1;
        }

        // Pad remaining positions with [PAD] token (ID 0)
        for (int i = realTokenCount; i < maxLength; i++)
        {
            inputIds[i] = _padTokenId;
            attentionMask[i] = 0;
            tokenTypeIds[i] = 0;
        }

        return (inputIds, attentionMask, tokenTypeIds);
    }

    /// <summary>
    /// Preprocesses text: lowercase, strip accents, normalize Unicode to NFC.
    /// </summary>
    private static string Preprocess(string text)
    {
        // Normalize to NFC
        text = text.Normalize(NormalizationForm.FormC);

        // Lowercase
        text = text.ToLowerInvariant();

        // Strip accents
        text = StripAccents(text);

        return text;
    }

    /// <summary>
    /// Strips diacritical marks (accents) from characters.
    /// </summary>
    private static string StripAccents(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (char c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Splits text on whitespace and punctuation, keeping punctuation as separate tokens.
    /// </summary>
    private static List<string> SplitOnWhitespaceAndPunctuation(string text)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else if (IsPunctuation(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                tokens.Add(c.ToString());
            }
            else if (IsControlOrUnusable(c))
            {
                // Skip control characters
                continue;
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    /// <summary>
    /// Checks if a character is punctuation per BERT tokenizer definition.
    /// </summary>
    private static bool IsPunctuation(char c)
    {
        int cp = c;

        // ASCII punctuation ranges
        if ((cp >= 33 && cp <= 47) || (cp >= 58 && cp <= 64) ||
            (cp >= 91 && cp <= 96) || (cp >= 123 && cp <= 126))
        {
            return true;
        }

        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category == UnicodeCategory.ConnectorPunctuation ||
               category == UnicodeCategory.DashPunctuation ||
               category == UnicodeCategory.OpenPunctuation ||
               category == UnicodeCategory.ClosePunctuation ||
               category == UnicodeCategory.InitialQuotePunctuation ||
               category == UnicodeCategory.FinalQuotePunctuation ||
               category == UnicodeCategory.OtherPunctuation;
    }

    /// <summary>
    /// Checks if a character is a control character or otherwise unusable.
    /// </summary>
    private static bool IsControlOrUnusable(char c)
    {
        if (c == '\t' || c == '\n' || c == '\r')
            return false; // These are treated as whitespace

        var category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category == UnicodeCategory.Control ||
               category == UnicodeCategory.Format;
    }

    /// <summary>
    /// Performs WordPiece tokenization on a single word.
    /// Returns a list of token IDs for the sub-word tokens.
    /// </summary>
    private List<int> WordPieceTokenize(string word)
    {
        if (string.IsNullOrEmpty(word))
            return new List<int>();

        // Check if the whole word is in vocab
        if (_vocab.TryGetValue(word, out int wholeWordId))
        {
            return new List<int> { wholeWordId };
        }

        var tokens = new List<int>();
        int start = 0;
        bool isFirstSubword = true;

        while (start < word.Length)
        {
            int end = word.Length;
            int foundId = -1;
            string foundToken = string.Empty;

            while (start < end)
            {
                string substr = word.Substring(start, end - start);
                if (!isFirstSubword)
                {
                    substr = "##" + substr;
                }

                if (_vocab.TryGetValue(substr, out int id))
                {
                    foundId = id;
                    foundToken = substr;
                    break;
                }

                end--;
            }

            if (foundId < 0)
            {
                // Cannot tokenize this word at all — replace entire word with [UNK]
                return new List<int> { _unkTokenId };
            }

            tokens.Add(foundId);
            start = end;
            isFirstSubword = false;
        }

        return tokens;
    }
}
