using ReadBack.Core.Models;

namespace ReadBack.Core.Chunking;

public interface ISpeechChunker
{
    IReadOnlyList<SpeechChunk> SplitIntoChunks(string text, int maxCharsPerChunk = 350);
}
