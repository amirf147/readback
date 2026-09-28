// Copyright 2026 Amir Farhadi
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// SPDX-License-Identifier: Apache-2.0
using System.Text.RegularExpressions;
using ReadBack.Core.Models;

namespace ReadBack.Core.Chunking;

public class SpeechChunker : ISpeechChunker
{
    private static readonly Regex SentenceSplitter = new(
        @"(?<=[.!?;\n])\s+",
        RegexOptions.Compiled
    );

    public IReadOnlyList<SpeechChunk> SplitIntoChunks(string text, int maxCharsPerChunk = 350)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<SpeechChunk>();

        var rawParagraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var chunkTexts = new List<string>();

        foreach (var paragraph in rawParagraphs)
        {
            if (paragraph.Length <= maxCharsPerChunk)
            {
                chunkTexts.Add(paragraph);
                continue;
            }

            var sentences = SentenceSplitter.Split(paragraph);
            var currentBuffer = new System.Text.StringBuilder();

            foreach (var sentence in sentences)
            {
                var trimmed = sentence.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                if (currentBuffer.Length + trimmed.Length + 1 <= maxCharsPerChunk)
                {
                    if (currentBuffer.Length > 0) currentBuffer.Append(' ');
                    currentBuffer.Append(trimmed);
                }
                else
                {
                    if (currentBuffer.Length > 0)
                    {
                        chunkTexts.Add(currentBuffer.ToString());
                        currentBuffer.Clear();
                    }

                    if (trimmed.Length <= maxCharsPerChunk)
                    {
                        currentBuffer.Append(trimmed);
                    }
                    else
                    {
                        var words = trimmed.Split(' ');
                        foreach (var word in words)
                        {
                            if (currentBuffer.Length + word.Length + 1 <= maxCharsPerChunk)
                            {
                                if (currentBuffer.Length > 0) currentBuffer.Append(' ');
                                currentBuffer.Append(word);
                            }
                            else
                            {
                                if (currentBuffer.Length > 0)
                                {
                                    chunkTexts.Add(currentBuffer.ToString());
                                    currentBuffer.Clear();
                                }
                                currentBuffer.Append(word);
                            }
                        }
                    }
                }
            }

            if (currentBuffer.Length > 0)
            {
                chunkTexts.Add(currentBuffer.ToString());
            }
        }

        var result = new List<SpeechChunk>(chunkTexts.Count);
        for (int i = 0; i < chunkTexts.Count; i++)
        {
            result.Add(new SpeechChunk
            {
                Index = i,
                TotalCount = chunkTexts.Count,
                Text = chunkTexts[i]
            });
        }

        return result;
    }
}
