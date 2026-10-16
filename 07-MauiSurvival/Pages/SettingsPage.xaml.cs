namespace Survival.Pages;

/// <summary>Налаштування-заглушка: ползунки гучності та перемикач підказок зберігаються в Preferences.</summary>
public partial class SettingsPage : ContentPage
{
    private bool _loading = true;

    public SettingsPage()
    {
        InitializeComponent();

        MusicSlider.Value = Preferences.Get("music", 70.0);
        SoundSlider.Value = Preferences.Get("sound", 70.0);
        ShowHints(Preferences.Get("hints", true));
        _loading = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = Services.Fx.EnterAsync(this);
    }

    private void OnMusicChanged(object? sender, ValueChangedEventArgs e)
    {
        MusicLabel.Text = $"МУЗИКА: {(int)e.NewValue}";
        if (!_loading) Preferences.Set("music", e.NewValue);
    }

    private void OnSoundChanged(object? sender, ValueChangedEventArgs e)
    {
        SoundLabel.Text = $"ЗВУКИ: {(int)e.NewValue}";
        if (!_loading) Preferences.Set("sound", e.NewValue);
    }

    private void OnHintsClicked(object? sender, EventArgs e)
    {
        var on = !Preferences.Get("hints", true);
        Preferences.Set("hints", on);
        ShowHints(on);
    }

    private void ShowHints(bool on) => HintsButton.Text = on ? "ПІДКАЗКИ: УВІМК." : "ПІДКАЗКИ: ВИМК.";

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
