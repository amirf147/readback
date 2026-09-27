using ReadBack.Core.Chunking;
using Xunit;

namespace ReadBack.Tests;

public class SpeechChunkerTests
{
    private readonly SpeechChunker _chunker = new();

    [Fact]
    public void SplitIntoChunks_HandlesEmptyText()
    {
        var chunks = _chunker.SplitIntoChunks("");
        Assert.Empty(chunks);
    }

    [Fact]
    public void SplitIntoChunks_SplitsOnParagraphs()
    {
        string text = "Paragraph one with some nice text.\n\nParagraph two with another thought.";
        var chunks = _chunker.SplitIntoChunks(text, maxCharsPerChunk: 300);

        Assert.Equal(2, chunks.Count);
        Assert.Equal(0, chunks[0].Index);
        Assert.Equal(1, chunks[1].Index);
        Assert.Equal(2, chunks[0].TotalCount);
        Assert.Contains("Paragraph one", chunks[0].Text);
        Assert.Contains("Paragraph two", chunks[1].Text);
    }

    [Fact]
    public void SplitIntoChunks_SplitsLongParagraphIntoSentences()
    {
        string text = "Sentence number one is short. Sentence number two has quite a few words inside it to test splitting. Sentence number three wraps up the discussion nicely.";
        var chunks = _chunker.SplitIntoChunks(text, maxCharsPerChunk: 70);

        Assert.True(chunks.Count >= 2);
        foreach (var chunk in chunks)
        {
            Assert.True(chunk.Text.Length <= 80);
        }
    }
}
