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
using System.Windows.Media;
using Xunit;
using Xunit.Abstractions;

namespace ReadBack.Tests;

public class AudioPlayerTests
{
    private readonly ITestOutputHelper _output;

    public AudioPlayerTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task MediaPlayer_PlaysMp3_Test()
    {
        // First synthesize an mp3
        var client = new ReadBack.Core.TTS.EdgeTTSClient();
        string tempMp3 = Path.Combine(Path.GetTempPath(), $"test_play_{Guid.NewGuid():N}.mp3");
        try
        {
            bool synthRes = await client.SynthesizeToFileAsync("Hello world", tempMp3, "en-US-ChristopherNeural", "+0%");
            Assert.True(synthRes);
            _output.WriteLine($"Synthesized {tempMp3}, size = {new FileInfo(tempMp3).Length}");

            // Now test playback with MediaPlayer
            var tcs = new TaskCompletionSource<bool>();
            var thread = new Thread(() =>
            {
                var player = new MediaPlayer();
                player.MediaOpened += (s, e) => _output.WriteLine("MediaOpened fired");
                player.MediaEnded += (s, e) => {
                    _output.WriteLine("MediaEnded fired");
                    tcs.TrySetResult(true);
                };
                player.MediaFailed += (s, e) => {
                    _output.WriteLine($"MediaFailed fired: {e.ErrorException}");
                    tcs.TrySetResult(false);
                };

                player.Open(new Uri(tempMp3, UriKind.Absolute));
                player.Play();

                // Run dispatcher for up to 5 seconds
                var dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(5);
                timer.Tick += (s, e) => {
                    _output.WriteLine("Timeout waiting for MediaEnded!");
                    tcs.TrySetResult(false);
                    dispatcher.InvokeShutdown();
                };
                timer.Start();

                tcs.Task.ContinueWith(_ => dispatcher.InvokeShutdown());
                System.Windows.Threading.Dispatcher.Run();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            bool ended = await tcs.Task;
            _output.WriteLine($"Playback finished: {ended}");
            Assert.True(ended);
        }
        finally
        {
            if (File.Exists(tempMp3)) File.Delete(tempMp3);
        }
    }
}
