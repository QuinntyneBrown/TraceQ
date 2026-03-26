using TraceQ.Infrastructure.Embeddings;

namespace TraceQ.Infrastructure.Tests.Embeddings;

public class WordPieceTokenizerTests
{
    /// <summary>
    /// Creates a small test vocabulary for unit testing.
    /// No dependency on the actual vocab.txt file.
    /// </summary>
    private static Dictionary<string, int> CreateTestVocab()
    {
        return new Dictionary<string, int>
        {
            ["[PAD]"] = 0,
            ["[UNK]"] = 100,
            ["[CLS]"] = 101,
            ["[SEP]"] = 102,
            ["hello"] = 7592,
            ["world"] = 2088,
            ["the"] = 1996,
            ["system"] = 2291,
            ["shall"] = 4618,
            ["provide"] = 3073,
            ["a"] = 1037,
            ["requirement"] = 9095,
            ["test"] = 3231,
            ["##ing"] = 2075,
            ["##s"] = 2015,
            ["##ed"] = 2098,
            ["##ment"] = 3672,
            ["require"] = 5765,
            ["embed"] = 11861,
            ["##ding"] = 4667,
            ["##d"] = 2094,
            ["."] = 1012,
            [","] = 1010,
            ["!"] = 999,
            ["("] = 1006,
            [")"] = 1007,
            ["-"] = 1011,
            ["air"] = 2250,
            ["gap"] = 6578,
            ["##ped"] = 5765,
            ["space"] = 2686,
            ["defense"] = 3639,
        };
    }

    private static WordPieceTokenizer CreateTokenizer(int maxLength = 32)
    {
        return new WordPieceTokenizer(CreateTestVocab(), maxLength);
    }

    [Fact]
    public void Tokenize_BasicText_ProducesCorrectStructure()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        var (inputIds, attentionMask, tokenTypeIds) = tokenizer.Tokenize("hello world");

        // First token should be [CLS] (101)
        Assert.Equal(101, inputIds[0]);

        // Should contain token IDs for "hello" and "world"
        Assert.Equal(7592, inputIds[1]); // hello
        Assert.Equal(2088, inputIds[2]); // world

        // Last real token should be [SEP] (102)
        Assert.Equal(102, inputIds[3]);

        // Total length should be maxLength
        Assert.Equal(16, inputIds.Length);
        Assert.Equal(16, attentionMask.Length);
        Assert.Equal(16, tokenTypeIds.Length);
    }

    [Fact]
    public void Tokenize_BasicText_HasCorrectAttentionMask()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize("hello world");

        // [CLS], hello, world, [SEP] = 4 real tokens
        Assert.Equal(1, attentionMask[0]); // [CLS]
        Assert.Equal(1, attentionMask[1]); // hello
        Assert.Equal(1, attentionMask[2]); // world
        Assert.Equal(1, attentionMask[3]); // [SEP]

        // Remaining should be 0 (padding)
        for (int i = 4; i < 16; i++)
        {
            Assert.Equal(0, attentionMask[i]);
        }
    }

    [Fact]
    public void Tokenize_BasicText_TokenTypeIdsAllZeros()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        var (_, _, tokenTypeIds) = tokenizer.Tokenize("hello world");

        // All token_type_ids should be 0 for single-sentence input
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal(0, tokenTypeIds[i]);
        }
    }

    [Fact]
    public void Tokenize_Truncation_RespectsMaxLength()
    {
        var tokenizer = CreateTokenizer(maxLength: 4);

        // "hello world" has 2 word tokens + CLS + SEP = 4 tokens total
        // With maxLength=4, it should truncate and still end with SEP
        var (inputIds, attentionMask, _) = tokenizer.Tokenize("hello world test");

        Assert.Equal(4, inputIds.Length);

        // Should start with [CLS]
        Assert.Equal(101, inputIds[0]);

        // Should end with [SEP] within the real tokens
        // Find the last non-padding token
        int lastRealIdx = -1;
        for (int i = inputIds.Length - 1; i >= 0; i--)
        {
            if (attentionMask[i] == 1)
            {
                lastRealIdx = i;
                break;
            }
        }

        Assert.True(lastRealIdx >= 0, "Should have at least one real token");
        Assert.Equal(102, inputIds[lastRealIdx]); // Last real token is [SEP]
    }

    [Fact]
    public void Tokenize_EmptyInput_HasOnlyCLSAndSEP()
    {
        var tokenizer = CreateTokenizer(maxLength: 8);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize("");

        // Should have [CLS] and [SEP] only
        Assert.Equal(101, inputIds[0]); // [CLS]
        Assert.Equal(102, inputIds[1]); // [SEP]

        Assert.Equal(1, attentionMask[0]);
        Assert.Equal(1, attentionMask[1]);

        // Rest should be padding
        for (int i = 2; i < 8; i++)
        {
            Assert.Equal(0, attentionMask[i]);
            Assert.Equal(0, inputIds[i]); // [PAD]
        }
    }

    [Fact]
    public void Tokenize_WhitespaceOnly_HasOnlyCLSAndSEP()
    {
        var tokenizer = CreateTokenizer(maxLength: 8);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize("   ");

        Assert.Equal(101, inputIds[0]); // [CLS]
        Assert.Equal(102, inputIds[1]); // [SEP]
        Assert.Equal(1, attentionMask[0]);
        Assert.Equal(1, attentionMask[1]);

        for (int i = 2; i < 8; i++)
        {
            Assert.Equal(0, attentionMask[i]);
        }
    }

    [Fact]
    public void Tokenize_NullInput_HasOnlyCLSAndSEP()
    {
        var tokenizer = CreateTokenizer(maxLength: 8);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize(null!);

        Assert.Equal(101, inputIds[0]); // [CLS]
        Assert.Equal(102, inputIds[1]); // [SEP]
        Assert.Equal(1, attentionMask[0]);
        Assert.Equal(1, attentionMask[1]);
    }

    [Fact]
    public void Tokenize_UnknownWords_ProducesUNKToken()
    {
        var tokenizer = CreateTokenizer(maxLength: 8);

        // "xyzzy" is not in our test vocabulary
        var (inputIds, _, _) = tokenizer.Tokenize("xyzzy");

        Assert.Equal(101, inputIds[0]);  // [CLS]
        Assert.Equal(100, inputIds[1]);  // [UNK]
        Assert.Equal(102, inputIds[2]);  // [SEP]
    }

    [Fact]
    public void Tokenize_Punctuation_SeparatedAsTokens()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize("hello, world!");

        Assert.Equal(101, inputIds[0]);  // [CLS]
        Assert.Equal(7592, inputIds[1]); // hello
        Assert.Equal(1010, inputIds[2]); // ,
        Assert.Equal(2088, inputIds[3]); // world
        Assert.Equal(999, inputIds[4]);  // !
        Assert.Equal(102, inputIds[5]);  // [SEP]
    }

    [Fact]
    public void Tokenize_SubwordTokenization_CorrectlySplits()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        // "embedding" should be tokenized as "embed" + "##ding"
        var (inputIds, _, _) = tokenizer.Tokenize("embedding");

        Assert.Equal(101, inputIds[0]);    // [CLS]
        Assert.Equal(11861, inputIds[1]);  // embed
        Assert.Equal(4667, inputIds[2]);   // ##ding
        Assert.Equal(102, inputIds[3]);    // [SEP]
    }

    [Fact]
    public void Tokenize_Uppercase_ConvertedToLowercase()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        var (inputIds, _, _) = tokenizer.Tokenize("HELLO WORLD");

        Assert.Equal(101, inputIds[0]);  // [CLS]
        Assert.Equal(7592, inputIds[1]); // hello (lowercased)
        Assert.Equal(2088, inputIds[2]); // world (lowercased)
        Assert.Equal(102, inputIds[3]);  // [SEP]
    }

    [Fact]
    public void Tokenize_OutputArraysArePaddedToMaxLength()
    {
        var tokenizer = CreateTokenizer(maxLength: 32);

        var (inputIds, attentionMask, tokenTypeIds) = tokenizer.Tokenize("hello");

        // All arrays should be exactly maxLength
        Assert.Equal(32, inputIds.Length);
        Assert.Equal(32, attentionMask.Length);
        Assert.Equal(32, tokenTypeIds.Length);

        // Real tokens: [CLS], hello, [SEP] = 3
        int realTokens = attentionMask.Count(m => m == 1);
        Assert.Equal(3, realTokens);

        // Padding tokens: all should be [PAD] = 0
        for (int i = 3; i < 32; i++)
        {
            Assert.Equal(0, inputIds[i]);
        }
    }

    [Fact]
    public void Tokenize_SpecialCharacters_HandledGracefully()
    {
        var tokenizer = CreateTokenizer(maxLength: 16);

        // Should not throw
        var (inputIds, attentionMask, _) = tokenizer.Tokenize("test\t\n\r test");

        // Should have at least CLS and SEP
        Assert.Equal(101, inputIds[0]);
        int realTokens = attentionMask.Count(m => m == 1);
        Assert.True(realTokens >= 2); // At minimum [CLS] and [SEP]
    }

    [Fact]
    public void Tokenize_VeryLongText_TruncatedCorrectly()
    {
        var tokenizer = CreateTokenizer(maxLength: 6);

        // "the system shall provide a requirement" = 6 words
        // With maxLength=6: [CLS] + 4 word tokens + [SEP] = 6
        var (inputIds, attentionMask, _) = tokenizer.Tokenize("the system shall provide a requirement");

        Assert.Equal(6, inputIds.Length);
        Assert.Equal(101, inputIds[0]); // [CLS]

        // Last real token must be [SEP]
        int lastRealIdx = -1;
        for (int i = 5; i >= 0; i--)
        {
            if (attentionMask[i] == 1)
            {
                lastRealIdx = i;
                break;
            }
        }

        Assert.Equal(102, inputIds[lastRealIdx]); // [SEP]

        // All attention positions used (fully filled after truncation)
        int realTokens = attentionMask.Count(m => m == 1);
        Assert.Equal(6, realTokens);
    }

    [Fact]
    public void Tokenize_MaxLengthOf2_HasOnlyCLSAndSEP()
    {
        var tokenizer = CreateTokenizer(maxLength: 2);

        var (inputIds, attentionMask, _) = tokenizer.Tokenize("hello world");

        Assert.Equal(2, inputIds.Length);
        Assert.Equal(101, inputIds[0]); // [CLS]
        Assert.Equal(102, inputIds[1]); // [SEP]
        Assert.Equal(1, attentionMask[0]);
        Assert.Equal(1, attentionMask[1]);
    }
}
