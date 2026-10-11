using System.Collections.Concurrent;
using ReadBack.Core.Chunking;
using ReadBack.Core.Filters;
using ReadBack.Core.Models;
using ReadBack.Core.Services;
using ReadBack.Core.TTS;

namespace ReadBack.Tests;

public class PlaybackControllerTests
{
    [Fact]
    public async Task Stop_PreservesCurrentPositionAndSameTextResumesThere()
    {
        using var harness = new PlaybackHarness();

        const string text = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";
        await harness.Controller.PlayTextAsync(text);
        Assert.Equal("Paragraph one.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());

        harness.Controller.NextChunk();
        Assert.Equal("Paragraph two.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());
        harness.Controller.Stop();

        Assert.Equal(PlaybackState.Stopped, harness.Controller.State);
        Assert.Equal(1, harness.Controller.CurrentIndex);

        await harness.Controller.PlayTextAsync(text);

        Assert.Equal(1, harness.Controller.CurrentIndex);
        Assert.Equal("Paragraph two.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task PreviousChunk_FirstPressRestartsCurrentAndSecondPressMovesBack()
    {
        using var harness = new PlaybackHarness();

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.\n\nParagraph three.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());

        harness.Controller.NextChunk();
        Assert.Equal("Paragraph two.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();
        Assert.Equal(1, harness.Controller.CurrentIndex);
        Assert.Equal("Paragraph two.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();
        Assert.Equal(0, harness.Controller.CurrentIndex);
        Assert.Equal("Paragraph one.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task PreviousChunk_AtFirstChunkRestartsWithoutGoingOutOfRange()
    {
        using var harness = new PlaybackHarness();

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();

        Assert.Equal(0, harness.Controller.CurrentIndex);
        Assert.Equal(PlaybackState.Playing, harness.Controller.State);
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task Playback_WaitsForSynthesisLongerThanFiveSeconds()
    {
        var synthesisGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new PlaybackHarness(async (text, outputPath, ct) =>
        {
            bool success = await synthesisGate.Task.WaitAsync(ct);
            if (success)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                await File.WriteAllTextAsync(outputPath, text, ct);
            }
            return success;
        });

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.");
        await Task.Delay(TimeSpan.FromSeconds(5.2));
        Assert.Equal(0, harness.AudioStartCount);

        synthesisGate.TrySetResult(true);

        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task RapidNextCommands_DoNotLetCanceledPlaybackAdvanceOverTarget()
    {
        using var harness = new PlaybackHarness();

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.\n\nParagraph three.\n\nParagraph four.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());

        harness.Controller.NextChunk();
        harness.Controller.NextChunk();

        Assert.Equal(2, harness.Controller.CurrentIndex);
        Assert.Equal("Paragraph three.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_2_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task PreviousTwiceAfterDelayStillMovesToPreviousChunk()
    {
        using var harness = new PlaybackHarness();

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.\n\nParagraph three.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
        harness.Controller.NextChunk();
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());
        await Task.Delay(1400);
        harness.Controller.PreviousChunk();

        Assert.Equal("Paragraph one.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task PreviousTwiceStillGoesBackIfFirstRestartFinishesBeforeSecondPress()
    {
        using var harness = new PlaybackHarness();

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.\n\nParagraph three.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
        harness.Controller.NextChunk();
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());
        harness.CompleteCurrentAudio();
        Assert.Contains("chunk_2_", await harness.WaitForAudioStartAsync());

        harness.Controller.PreviousChunk();

        Assert.Equal("Paragraph one.", harness.Controller.CurrentChunk!.Text);
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
    }

    [Fact]
    public async Task PlaybackCompletesAndClearsPositionAfterLastChunkFinishes()
    {
        using var harness = new PlaybackHarness();
        var playbackFinished = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Controller.StateChanged += (_, args) =>
        {
            if (args.NewState == PlaybackState.Idle)
                playbackFinished.TrySetResult(true);
        };

        await harness.Controller.PlayTextAsync("Paragraph one.\n\nParagraph two.");
        Assert.Contains("chunk_0_", await harness.WaitForAudioStartAsync());
        harness.CompleteCurrentAudio();
        Assert.Contains("chunk_1_", await harness.WaitForAudioStartAsync());
        harness.CompleteCurrentAudio();

        Assert.True(await playbackFinished.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Equal(-1, harness.Controller.CurrentIndex);
    }

    private sealed class PlaybackHarness : IDisposable
    {
        private readonly ConcurrentQueue<string> _audioStarts = new();
        private readonly SemaphoreSlim _audioStarted = new(0);
        private readonly object _audioLock = new();
        private TaskCompletionSource<bool>? _activeAudio;

        public PlaybackHarness(
            Func<string, string, CancellationToken, Task<bool>>? synthesize = null)
        {
            var ttsEngine = new FakeTtsEngine(synthesize);
            Controller = new PlaybackController(
                new NarrationPipeline(),
                new SpeechChunker(),
                ttsEngine,
                new FakeSettingsService(),
                PlayAudioAsync,
                StopAudio,
                () => { },
                () => { });
        }

        public PlaybackController Controller { get; }
        public int AudioStartCount => _audioStarts.Count;

        public void CompleteCurrentAudio()
        {
            lock (_audioLock)
                _activeAudio?.TrySetResult(true);
        }

        public async Task<string> WaitForAudioStartAsync()
        {
            Assert.True(await _audioStarted.WaitAsync(TimeSpan.FromSeconds(5)), "Audio playback did not start.");
            Assert.True(_audioStarts.TryDequeue(out var path));
            return path!;
        }

        private Task PlayAudioAsync(string filePath)
        {
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_audioLock)
                _activeAudio = completion;

            _audioStarts.Enqueue(filePath);
            _audioStarted.Release();
            return completion.Task;
        }

        private void StopAudio()
        {
            lock (_audioLock)
                _activeAudio?.TrySetCanceled();
        }

        public void Dispose()
        {
            Controller.Stop();
            _audioStarted.Dispose();
        }
    }

    private sealed class FakeSettingsService : ISettingsService
    {
        public AppSettings CurrentSettings { get; } = new();

        public void Save() { }

        public event EventHandler<AppSettings>? SettingsChanged
        {
            add { }
            remove { }
        }
    }

    private sealed class FakeTtsEngine : ITTSEngine
    {
        private readonly Func<string, string, CancellationToken, Task<bool>>? _synthesize;

        public FakeTtsEngine(Func<string, string, CancellationToken, Task<bool>>? synthesize)
        {
            _synthesize = synthesize;
        }

        public string Name => "fake-engine";

        public async Task<bool> SynthesizeToFileAsync(
            string text,
            string outputPath,
            string voiceId,
            string rate,
            CancellationToken ct = default)
        {
            if (_synthesize != null)
                return await _synthesize(text, outputPath, ct);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await File.WriteAllTextAsync(outputPath, text, ct);
            return true;
        }

        public Task<IReadOnlyList<VoiceInfo>> GetAvailableVoicesAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<VoiceInfo>>(Array.Empty<VoiceInfo>());
        }
    }
}
