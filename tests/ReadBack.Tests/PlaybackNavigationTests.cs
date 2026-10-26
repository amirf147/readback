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
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadBack.Core.Chunking;
using ReadBack.Core.Filters;
using ReadBack.Core.Models;
using ReadBack.Core.Services;
using ReadBack.Core.TTS;
using Xunit;

namespace ReadBack.Tests;

public class PlaybackNavigationTests
{
    private class FakePipeline : INarrationPipeline
    {
        public IReadOnlyList<INarrationFilter> Filters => Array.Empty<INarrationFilter>();
        public void RegisterFilter(INarrationFilter filter) { }
        public bool RemoveFilter(string filterName) => true;
        public string Process(string text, FilterContext context) => text;
    }

    private class FakeChunker : ISpeechChunker
    {
        public IReadOnlyList<SpeechChunk> SplitIntoChunks(string text, int maxCharsPerChunk = 350)
        {
            var parts = text.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<SpeechChunk>();
            for (int i = 0; i < parts.Length; i++)
            {
                list.Add(new SpeechChunk
                {
                    Index = i,
                    TotalCount = parts.Length,
                    Text = parts[i]
                });
            }
            return list;
        }
    }

    private class FakeTtsEngine : ITTSEngine
    {
        public string Name => "Fake";

        public Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<VoiceInfo>>(Array.Empty<VoiceInfo>());

        public Task<bool> SynthesizeToFileAsync(string text, string outputPath, string voiceId, string rate, CancellationToken ct = default)
        {
            File.WriteAllBytes(outputPath, new byte[] { 1, 2, 3, 4 });
            return Task.FromResult(true);
        }
    }

    private class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; set; } = new AppSettings();
        public void Save() { }
        public event EventHandler<AppSettings>? SettingsChanged { add { } remove { } }
    }

    [Fact]
    public async Task NextChunk_IncrementsByExactlyOne_DoesNotSkip()
    {
        var pipeline = new FakePipeline();
        var chunker = new FakeChunker();
        var tts = new FakeTtsEngine();
        var settings = new FakeSettingsService();

        TaskCompletionSource<bool>? activePlayTcs = null;
        var playLock = new object();

        Func<string, Task> playDelegate = path =>
        {
            lock (playLock)
            {
                activePlayTcs = new TaskCompletionSource<bool>();
            }
            return activePlayTcs.Task;
        };

        Action stopDelegate = () =>
        {
            lock (playLock)
            {
                activePlayTcs?.TrySetCanceled();
            }
        };

        var controller = new PlaybackController(
            pipeline,
            chunker,
            tts,
            settings,
            playDelegate,
            stopDelegate,
            () => { },
            () => { }
        );

        string text = "Paragraph 1\n\nParagraph 2\n\nParagraph 3";
        await controller.PlayTextAsync(text);

        // Wait until Chunk 0 starts playing
        int waitCount = 0;
        while (controller.CurrentIndex != 0 && waitCount < 50)
        {
            await Task.Delay(20);
            waitCount++;
        }
        Assert.Equal(0, controller.CurrentIndex);

        // User hits Next -> Must advance to Paragraph 2 (index 1), NEVER index 2
        controller.NextChunk();

        // Wait for loop to transition
        waitCount = 0;
        while (controller.CurrentIndex == 0 && waitCount < 50)
        {
            await Task.Delay(20);
            waitCount++;
        }

        Assert.Equal(1, controller.CurrentIndex);
        Assert.Equal("Paragraph 2", controller.CurrentChunk?.Text);

        // User hits Next again -> Must advance to Paragraph 3 (index 2)
        controller.NextChunk();

        waitCount = 0;
        while (controller.CurrentIndex == 1 && waitCount < 50)
        {
            await Task.Delay(20);
            waitCount++;
        }

        Assert.Equal(2, controller.CurrentIndex);
        Assert.Equal("Paragraph 3", controller.CurrentChunk?.Text);

        // First press restarts Paragraph 3 (index 2)
        controller.PreviousChunk();
        Assert.Equal(2, controller.CurrentIndex);

        // Second press within 3 seconds retreats to Paragraph 2 (index 1)
        controller.PreviousChunk();

        waitCount = 0;
        while (controller.CurrentIndex == 2 && waitCount < 50)
        {
            await Task.Delay(20);
            waitCount++;
        }

        Assert.Equal(1, controller.CurrentIndex);
        Assert.Equal("Paragraph 2", controller.CurrentChunk?.Text);

        controller.Stop();
        Assert.Equal(PlaybackState.Stopped, controller.State);
    }
}
