using System.ComponentModel;

namespace VoiceNotes.Models;

/// <summary>
/// Один запис у списку. INotifyPropertyChanged потрібен, щоб екран сам оновлювався,
/// коли змінюються текст від ШІ, стан «відтворюється» або «ШІ думає».
/// </summary>
public class Recording : INotifyPropertyChanged
{
    public string Path { get; init; } = "";
    public string Name => System.IO.Path.GetFileName(Path);
    public DateTime Created { get; init; }
    public TimeSpan Duration { get; init; }

    public string Title => Created.ToString("dd.MM.yyyy  HH:mm:ss");
    public string DurationText => $"{(int)Duration.TotalMinutes:D2}:{Duration.Seconds:D2}";

    private bool _isPlaying;
    public bool IsPlaying
    {
        get => _isPlaying;
        set { _isPlaying = value; Notify(nameof(IsPlaying)); Notify(nameof(PlayIcon)); }
    }
    public string PlayIcon => IsPlaying ? "icon_stop.png" : "icon_play.png";

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; Notify(nameof(IsBusy)); Notify(nameof(CanSend)); }
    }
    public bool CanSend => !IsBusy;

    /// <summary>Текст запису: його можна вписати чи вставити вручну (наприклад, якщо модель зі звуком недоступна) і попросити ШІ скласти тези.</summary>
    public string Transcript { get; set; } = "";

    private string _aiText = "";
    public string AiText
    {
        get => _aiText;
        set { _aiText = value; Notify(nameof(AiText)); Notify(nameof(HasAiText)); }
    }
    public bool HasAiText => !string.IsNullOrEmpty(AiText);

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
