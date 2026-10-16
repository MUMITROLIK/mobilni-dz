#if ANDROID
using Android.Media;
#elif WINDOWS
using NAudio.Wave;
#endif

namespace VoiceNotes.Services;

/// <summary>
/// Запис з мікрофона і відтворення. Мікрофон - це платформена річ, тому у файлі є дві реалізації:
/// на Android використовується MediaRecorder (код викладача, формат .m4a), на Windows - бібліотека NAudio (формат .wav).
/// Решта програми не знає про різницю: вона користується лише цими методами.
/// </summary>
public class AudioRecorder
{
    /// <summary>Розширення файлу для поточної платформи.</summary>
#if ANDROID
    public const string Extension = ".m4a";
    public const string AudioFormat = "m4a";
#else
    public const string Extension = ".wav";
    public const string AudioFormat = "wav";
#endif

    public bool IsRecording { get; private set; }

    /// <summary>Поточна гучність від 0 до 1 (для анімації «еквалайзера»).</summary>
    public double Level { get; private set; }

    /// <summary>Спрацьовує, коли відтворення закінчилося саме.</summary>
    public event Action? PlaybackFinished;

#if ANDROID
    private MediaRecorder? _recorder;
    private MediaPlayer? _player;

    public async Task<string?> StartAsync(string filePath)
    {
        // Без дозволу користувача мікрофон недоступний: показуємо системне вікно
        var status = await Permissions.RequestAsync<Permissions.Microphone>();
        if (status != PermissionStatus.Granted) return "Доступ до мікрофона заборонено.";

        _recorder = OperatingSystem.IsAndroidVersionAtLeast(31) ? new MediaRecorder(Android.App.Application.Context) : new MediaRecorder();
        _recorder.SetAudioSource(AudioSource.Mic);
        _recorder.SetOutputFormat(OutputFormat.Mpeg4);
        _recorder.SetAudioEncoder(AudioEncoder.Aac);
        _recorder.SetOutputFile(filePath);
        _recorder.Prepare();
        _recorder.Start();
        IsRecording = true;
        return null;
    }

    public void UpdateLevel() => Level = _recorder is null ? 0 : Math.Min(1, _recorder.MaxAmplitude / 12000.0);

    public Task StopAsync()
    {
        if (_recorder is not null)
        {
            _recorder.Stop();
            _recorder.Release();
            _recorder.Dispose();
            _recorder = null;
        }
        IsRecording = false;
        Level = 0;
        return Task.CompletedTask;
    }

    public TimeSpan GetDuration(string path)
    {
        try
        {
            using var retriever = new MediaMetadataRetriever();
            retriever.SetDataSource(path);
            var ms = retriever.ExtractMetadata(MetadataKey.Duration);
            return TimeSpan.FromMilliseconds(long.TryParse(ms, out var v) ? v : 0);
        }
        catch (Exception) { return TimeSpan.Zero; }
    }

    public void Play(string path)
    {
        StopPlayback();
        _player = new MediaPlayer();
        _player.SetDataSource(path);
        _player.Prepare();
        _player.Completion += (_, _) => { StopPlayback(); PlaybackFinished?.Invoke(); };
        _player.Start();
    }

    public void StopPlayback()
    {
        _player?.Release();
        _player?.Dispose();
        _player = null;
    }

#elif WINDOWS
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _writer;
    private TaskCompletionSource? _stopped;
    private WaveOutEvent? _output;
    private AudioFileReader? _reader;

    public Task<string?> StartAsync(string filePath)
    {
        if (WaveInEvent.DeviceCount == 0)
            return Task.FromResult<string?>("Мікрофон не знайдено. Перевірте, що він підключений і дозволений у налаштуваннях Windows.");

        // 16 кГц, 16 біт, моно: мало місця, але для мови цього досить
        _waveIn = new WaveInEvent { WaveFormat = new WaveFormat(16000, 16, 1), BufferMilliseconds = 60 };
        _writer = new WaveFileWriter(filePath, _waveIn.WaveFormat);
        _stopped = new TaskCompletionSource();

        _waveIn.DataAvailable += (_, e) =>
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);

            // Шукаємо найгучніший відлік у порції - це і є рівень для еквалайзера
            var peak = 0;
            for (var i = 0; i < e.BytesRecorded - 1; i += 2)
                peak = Math.Max(peak, Math.Abs(BitConverter.ToInt16(e.Buffer, i)));
            Level = Math.Min(1, peak / 12000.0);
        };
        _waveIn.RecordingStopped += (_, _) =>
        {
            _writer?.Dispose();
            _writer = null;
            _waveIn?.Dispose();
            _waveIn = null;
            _stopped?.TrySetResult();
        };

        try
        {
            _waveIn.StartRecording();
        }
        catch (Exception ex)
        {
            _writer.Dispose();
            _writer = null;
            _waveIn.Dispose();
            _waveIn = null;
            return Task.FromResult<string?>("Не вдалося почати запис: " + ex.Message);
        }

        IsRecording = true;
        return Task.FromResult<string?>(null);
    }

    public void UpdateLevel() { }   // на Windows рівень оновлюється в DataAvailable

    public async Task StopAsync()
    {
        _waveIn?.StopRecording();
        if (_stopped is not null) await _stopped.Task;   // чекаємо, поки файл повністю закриється
        IsRecording = false;
        Level = 0;
    }

    public TimeSpan GetDuration(string path)
    {
        try
        {
            using var reader = new AudioFileReader(path);
            return reader.TotalTime;
        }
        catch (Exception) { return TimeSpan.Zero; }
    }

    public void Play(string path)
    {
        StopPlayback();
        _reader = new AudioFileReader(path);
        _output = new WaveOutEvent();
        _output.Init(_reader);
        _output.PlaybackStopped += (_, _) => MainThread.BeginInvokeOnMainThread(() =>
        {
            StopPlayback();
            PlaybackFinished?.Invoke();
        });
        _output.Play();
    }

    public void StopPlayback()
    {
        var output = _output;
        _output = null;
        output?.Dispose();
        _reader?.Dispose();
        _reader = null;
    }

#else
    public Task<string?> StartAsync(string filePath) => Task.FromResult<string?>("Запис підтримується лише на Android і Windows.");
    public void UpdateLevel() { }
    public Task StopAsync() => Task.CompletedTask;
    public TimeSpan GetDuration(string path) => TimeSpan.Zero;
    public void Play(string path) { }
    public void StopPlayback() { }
#endif
}
