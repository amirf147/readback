namespace ReadBack.App.Services;

public interface IAudioPlayer
{
    Task PlayFileAsync(string filePath);
    void Pause();
    void Resume();
    void Stop();
}
