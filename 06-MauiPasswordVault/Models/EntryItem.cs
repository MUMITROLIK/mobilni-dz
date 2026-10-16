using System.ComponentModel;

namespace PassVault.Models;

/// <summary>
/// Те, що показується в списку: запис плюс розшифрований пароль і стан «показати/сховати».
/// Розшифрований пароль живе лише в пам'яті і ніколи не записується у файл.
/// </summary>
public class EntryItem : INotifyPropertyChanged
{
    public EntryItem(VaultEntry entry, string plainPassword)
    {
        Entry = entry;
        PlainPassword = plainPassword;
    }

    public VaultEntry Entry { get; }
    public string PlainPassword { get; }

    public string Name => Entry.Name;
    public string Subtitle => string.Join("  ·  ", new[] { Entry.Site, Entry.Login }.Where(s => !string.IsNullOrWhiteSpace(s)));
    public bool HasSubtitle => Subtitle.Length > 0;
    public string CategoryIcon => Category.ById(Entry.CategoryId).Icon;
    public string CategoryName => Category.ById(Entry.CategoryId).Name;
    public string TagText => string.IsNullOrWhiteSpace(Entry.Tag) ? "" : "#" + Entry.Tag.Trim();
    public bool HasTag => TagText.Length > 0;

    private bool _isRevealed;
    public bool IsRevealed
    {
        get => _isRevealed;
        set
        {
            _isRevealed = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRevealed)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PasswordShown)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EyeIcon)));
        }
    }

    /// <summary>Пароль зірочками, поки його не відкрили кнопкою-оком.</summary>
    public string PasswordShown => IsRevealed ? PlainPassword : new string('*', Math.Clamp(PlainPassword.Length, 4, 14));
    public string EyeIcon => IsRevealed ? "icon_eye_off.png" : "icon_eye.png";

    public event PropertyChangedEventHandler? PropertyChanged;
}
