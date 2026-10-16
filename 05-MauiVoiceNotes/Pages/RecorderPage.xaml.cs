using System.Collections.ObjectModel;
using System.Diagnostics;
using VoiceNotes.Models;
using VoiceNotes.Services;

namespace VoiceNotes.Pages;

public partial class RecorderPage : ContentPage
{
    private const int BarCount = 26;

    private readonly AudioRecorder _recorder = new();
    private readonly AiService _ai = new();
    private readonly ObservableCollection<Recording> _items = new();
    private readonly List<BoxView> _bars = new();
    private readonly double[] _levels = new double[BarCount];   // історія рівнів для еквалайзера
    private readonly Stopwatch _clock = new();
    private readonly IDispatcherTimer _ticker;
    private string? _currentPath;
    private bool _started;

    private static string RecordsFolder => Path.Combine(FileSystem.AppDataDirectory, "records");

    public RecorderPage()
    {
        InitializeComponent();
        RecordingsList.ItemsSource = _items;
        BuildBars();
        UpdateModelButton();

        // Кожні 70 мс оновлюємо час і еквалайзер
        _ticker = Dispatcher.CreateTimer();
        _ticker.Interval = TimeSpan.FromMilliseconds(70);
        _ticker.Tick += (_, _) => OnTick();
        _ticker.Start();

        _recorder.PlaybackFinished += () =>
        {
            foreach (var item in _items) item.IsPlaying = false;
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        Directory.CreateDirectory(RecordsFolder);
        LoadExistingRecordings();

        await _ai.LoadKeyAsync();
        if (!_ai.HasKey) KeyOverlay.IsVisible = true;
        ModelList.ItemsSource = await _ai.GetAudioModelsAsync();
    }

    /// <summary>Підхоплюємо записи, зроблені раніше (вони лежать у папці даних застосунку).</summary>
    private void LoadExistingRecordings()
    {
        var files = new DirectoryInfo(RecordsFolder).GetFiles("record_*" + AudioRecorder.Extension)
            .OrderByDescending(f => f.CreationTime);
        foreach (var file in files)
            _items.Add(new Recording { Path = file.FullName, Created = file.CreationTime, Duration = _recorder.GetDuration(file.FullName) });
    }

    // ---------- Запис ----------

    private async void OnRecordClicked(object? sender, EventArgs e)
    {
        if (_recorder.IsRecording) await StopRecordingAsync();
        else await StartRecordingAsync();
    }

    private async Task StartRecordingAsync()
    {
        StopAllPlayback();
        _currentPath = Path.Combine(RecordsFolder, $"record_{DateTime.Now:yyyyMMdd_HHmmss}{AudioRecorder.Extension}");

        var error = await _recorder.StartAsync(_currentPath);
        if (error is not null)
        {
            HintLabel.Text = error.ToUpperInvariant();
            HintLabel.TextColor = Color.FromArgb("#B13E53");
            return;
        }

        _clock.Restart();
        RecordButton.Source = "stop.png";
        RecordButton.BackgroundColor = Color.FromArgb("#B13E53");
        HintLabel.Text = "ЙДЕ ЗАПИС... НАТИСНИ, ЩОБ ЗУПИНИТИ";
        HintLabel.TextColor = Color.FromArgb("#FFCD75");
    }

    private async Task StopRecordingAsync()
    {
        await _recorder.StopAsync();
        _clock.Stop();

        RecordButton.Source = "mic.png";
        RecordButton.BackgroundColor = (Color)Application.Current!.Resources["Panel"];
        HintLabel.Text = "ЗБЕРЕЖЕНО! НАТИСНИ, ЩОБ ЗАПИСАТИ ЩЕ";
        HintLabel.TextColor = (Color)Application.Current!.Resources["Green"];

        if (_currentPath is not null && File.Exists(_currentPath))
        {
            var duration = _recorder.GetDuration(_currentPath);
            _items.Insert(0, new Recording { Path = _currentPath, Created = DateTime.Now, Duration = duration });
        }
    }

    /// <summary>Тік таймера: час запису і рух стовпчиків еквалайзера.</summary>
    private void OnTick()
    {
        double level = 0;
        if (_recorder.IsRecording)
        {
            _recorder.UpdateLevel();
            level = _recorder.Level;
            var t = _clock.Elapsed;
            TimeLabel.Text = $"{(int)t.TotalMinutes:D2}:{t.Seconds:D2}";
        }

        // Зсуваємо історію вліво і додаємо новий рівень справа
        Array.Copy(_levels, 1, _levels, 0, BarCount - 1);
        _levels[BarCount - 1] = level;

        for (var i = 0; i < BarCount; i++)
        {
            _bars[i].HeightRequest = 6 + _levels[i] * 58;
            _bars[i].Color = _recorder.IsRecording ? Color.FromArgb("#38B764") : Color.FromArgb("#333C57");
        }
    }

    private void BuildBars()
    {
        for (var i = 0; i < BarCount; i++)
        {
            var bar = new BoxView { WidthRequest = 14, HeightRequest = 6, VerticalOptions = LayoutOptions.End, Color = Color.FromArgb("#333C57"), CornerRadius = 0 };
            _bars.Add(bar);
            BarsRow.Add(bar);
        }
    }

    // ---------- Дії над записом ----------

    private static Recording? ItemOf(object? sender) => (sender as Button)?.BindingContext as Recording;

    private void OnPlayClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item || _recorder.IsRecording) return;

        var wasPlaying = item.IsPlaying;
        StopAllPlayback();
        if (wasPlaying) return;

        try
        {
            _recorder.Play(item.Path);
            item.IsPlaying = true;
        }
        catch (Exception ex)
        {
            item.AiText = "Не вдалося відтворити: " + ex.Message;
        }
    }

    private void StopAllPlayback()
    {
        _recorder.StopPlayback();
        foreach (var item in _items) item.IsPlaying = false;
    }

    /// <summary>Друга частина завдання: надіслати запис у ШІ.</summary>
    private async void OnAiClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item || item.IsBusy) return;

        if (!_ai.HasKey)
        {
            KeyOverlay.IsVisible = true;
            return;
        }

        item.IsBusy = true;
        item.AiText = "";
        try
        {
            item.AiText = await _ai.AnalyzeAsync(item.Path, AudioRecorder.AudioFormat);
        }
        catch (Exception ex)
        {
            item.AiText = "ПОМИЛКА: " + ex.Message;
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    /// <summary>Запасний шлях: ШІ складає тези з тексту запису, який ми вписали чи вставили вручну.</summary>
    private async void OnSummarizeClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item || item.IsBusy) return;

        if (string.IsNullOrWhiteSpace(item.Transcript))
        {
            item.AiText = "Спочатку впиши текст запису в поле вище.";
            return;
        }
        if (!_ai.HasKey)
        {
            KeyOverlay.IsVisible = true;
            return;
        }

        item.IsBusy = true;
        item.AiText = "";
        try
        {
            item.AiText = await _ai.SummarizeTextAsync(item.Transcript);
        }
        catch (Exception ex)
        {
            item.AiText = "ПОМИЛКА: " + ex.Message;
        }
        finally
        {
            item.IsBusy = false;
        }
    }

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item) return;
        if (item.IsPlaying) StopAllPlayback();

        try { File.Delete(item.Path); }
        catch (IOException) { await Task.Delay(100); }
        _items.Remove(item);
    }

    // ---------- Вибір моделі ----------

    private void OnModelClicked(object? sender, EventArgs e) => ModelOverlay.IsVisible = true;
    private void OnModelCloseClicked(object? sender, EventArgs e) => ModelOverlay.IsVisible = false;

    private void OnModelPicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is string model) _ai.Model = model;
        UpdateModelButton();
        ModelOverlay.IsVisible = false;
    }

    private void UpdateModelButton()
    {
        var name = _ai.Model.Split('/').Last().Replace(":free", "");
        ModelButton.Text = name.Length > 24 ? name[..24] + "…" : name;
    }

    // ---------- Ключ ----------

    private void OnKeySaveClicked(object? sender, EventArgs e)
    {
        var key = KeyEntry.Text?.Trim();
        if (string.IsNullOrEmpty(key)) return;
        _ai.SaveKey(key);
        KeyEntry.Text = "";
        KeyOverlay.IsVisible = false;
    }

    private void OnKeyLaterClicked(object? sender, EventArgs e) => KeyOverlay.IsVisible = false;
}
