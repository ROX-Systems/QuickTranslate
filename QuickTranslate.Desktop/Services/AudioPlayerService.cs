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
    private readonly object _lock = new();

    public bool IsPlaying
    {
        get
        {
            lock (_lock)
            {
                return _isPlaying;
            }
        }
    }
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

        MemoryStream? tempAudioStream = null;
        WaveFileReader? tempWaveReader = null;
        WaveOutEvent? tempWaveOut = null;

        try
        {
            lock (_lock)
            {
                _isPlaying = true;
            }
            _logger.Information("AudioPlayer: Playing {Size} bytes", wavData.Length);

            // Create audio stream and reader
            tempAudioStream = new MemoryStream(wavData);
            tempWaveReader = new WaveFileReader(tempAudioStream);
            tempWaveOut = new WaveOutEvent();

            // Hook up playback stopped event
            tempWaveOut.PlaybackStopped += (s, e) =>
            {
                _logger.Information("AudioPlayer: Playback finished");
                lock (_lock)
                {
                    if (_isPlaying)
                    {
                        _isPlaying = false;
                        PlaybackFinished?.Invoke(this, EventArgs.Empty);
                    }
                }
            };

            tempWaveOut.Init(tempWaveReader);
            tempWaveOut.Play();

            // Assign to instance variables only after successful initialization
            _audioStream = tempAudioStream;
            _waveReader = tempWaveReader;
            _waveOut = tempWaveOut;
            tempAudioStream = null;
            tempWaveReader = null;
            tempWaveOut = null;

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
            lock (_lock)
            {
                _isPlaying = false;
            }

            // Clean up temporary resources if initialization failed
            tempWaveOut?.Dispose();
            tempWaveReader?.Dispose();
            tempAudioStream?.Dispose();
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

        lock (_lock)
        {
            _isPlaying = false;
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
