using System.IO;
using System.Media;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private readonly ILogger _logger;
    private SoundPlayer? _soundPlayer;
    private CancellationTokenSource? _playbackCts;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;
    public event EventHandler? PlaybackFinished;

    public AudioPlayerService()
    {
        _logger = Log.ForContext<AudioPlayerService>();
    }

    public async Task PlayAsync(byte[] wavData, CancellationToken cancellationToken = default)
    {
        if (wavData == null || wavData.Length == 0)
        {
            _logger.Warning("AudioPlayer: No audio data to play");
            return;
        }

        Stop();

        _playbackCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _isPlaying = true;
            _logger.Information("AudioPlayer: Playing {Size} bytes", wavData.Length);

            using var ms = new MemoryStream(wavData);
            _soundPlayer = new SoundPlayer(ms);
            
            await Task.Run(() =>
            {
                _soundPlayer.PlaySync();
            }, _playbackCts.Token);

            _logger.Information("AudioPlayer: Playback finished");
        }
        catch (OperationCanceledException)
        {
            _logger.Information("AudioPlayer: Playback cancelled");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "AudioPlayer: Playback error");
        }
        finally
        {
            _isPlaying = false;
            _soundPlayer?.Dispose();
            _soundPlayer = null;
            PlaybackFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Stop()
    {
        if (_soundPlayer != null)
        {
            _logger.Information("AudioPlayer: Stopping playback");
            _playbackCts?.Cancel();
            _soundPlayer.Stop();
            _soundPlayer.Dispose();
            _soundPlayer = null;
            _isPlaying = false;
        }
    }

    public void Dispose()
    {
        Stop();
        _playbackCts?.Dispose();
    }
}
