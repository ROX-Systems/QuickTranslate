using System.IO;
using NAudio.Wave;
using QuickTranslate.Desktop.Services.Interfaces;
using Serilog;

namespace QuickTranslate.Desktop.Services;

public class AudioPlayerService : IAudioPlayerService
{
    private readonly ILogger _logger;
    private WaveOutEvent? _waveOut;
    private WaveFileReader? _waveReader;
    private MemoryStream? _audioStream;
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

        try
        {
            _isPlaying = true;
            _logger.Information("AudioPlayer: Playing {Size} bytes", wavData.Length);

            // Create audio stream and reader
            _audioStream = new MemoryStream(wavData);
            _waveReader = new WaveFileReader(_audioStream);
            _waveOut = new WaveOutEvent();
            
            // Hook up playback stopped event
            _waveOut.PlaybackStopped += (s, e) =>
            {
                _logger.Information("AudioPlayer: Playback finished");
                if (_isPlaying)
                {
                    _isPlaying = false;
                    PlaybackFinished?.Invoke(this, EventArgs.Empty);
                }
            };
            
            _waveOut.Init(_waveReader);
            _waveOut.Play();
            
            // Wait for playback to complete
            await Task.Run(() =>
            {
                while (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(50);
                }
            }, cancellationToken);
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
        }
    }

    public void Stop()
    {
        if (_waveOut != null)
        {
            _logger.Information("AudioPlayer: Stopping playback");
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }
        
        if (_waveReader != null)
        {
            _waveReader.Dispose();
            _waveReader = null;
        }
        
        if (_audioStream != null)
        {
            _audioStream.Dispose();
            _audioStream = null;
        }
        
        _isPlaying = false;
    }

    public void Dispose()
    {
        Stop();
    }
}
