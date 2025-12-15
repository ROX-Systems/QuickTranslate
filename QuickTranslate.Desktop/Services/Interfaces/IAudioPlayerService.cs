namespace QuickTranslate.Desktop.Services.Interfaces;

public interface IAudioPlayerService : IDisposable
{
    Task PlayAsync(byte[] wavData, CancellationToken cancellationToken = default);
    void Stop();
    bool IsPlaying { get; }
    event EventHandler? PlaybackFinished;
}
