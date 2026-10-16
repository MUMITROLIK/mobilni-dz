using Survival.Services;

namespace Survival.Pages;

public partial class MenuPage : ContentPage
{
    private CancellationTokenSource? _anim;

    public MenuPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.StartClock();   // запускаємо ігровий годинник один раз за запуск
        UpdateButtons();

        // Кнопки меню виїжджають по черзі, а вогнище мерехтить і розсипає іскри
        _ = Fx.StaggerAsync(MenuStack.Children.OfType<View>().Where(v => v is not Grid));
        _anim = new CancellationTokenSource();
        _ = FlickerAsync(_anim.Token);
        _ = EmbersAsync(_anim.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _anim?.Cancel();
    }

    /// <summary>Полум'я мерехтить: випадкові короткі зміни розміру, як у справжнього вогню.</summary>
    private async Task FlickerAsync(CancellationToken token)
    {
        var rnd = new Random();
        while (!token.IsCancellationRequested)
        {
            await Task.WhenAll(
                Fire.ScaleTo(0.97 + rnd.NextDouble() * 0.1, (uint)rnd.Next(110, 260), Easing.SinInOut),
                Fire.RotateTo(rnd.NextDouble() * 4 - 2, (uint)rnd.Next(110, 260), Easing.SinInOut));
        }
    }

    /// <summary>Іскри відлітають від вогню: маленькі квадрати летять угору, зсуваються вбік і гаснуть.</summary>
    private async Task EmbersAsync(CancellationToken token)
    {
        var rnd = new Random();
        string[] colors = { "#FFCD75", "#EF7D57", "#F4F4F4" };
        while (!token.IsCancellationRequested)
        {
            var size = rnd.Next(4, 8);
            var spark = new BoxView { WidthRequest = size, HeightRequest = size, Color = Color.FromArgb(colors[rnd.Next(colors.Length)]), InputTransparent = true };
            AbsoluteLayout.SetLayoutBounds(spark, new Rect(52 + rnd.Next(-14, 14), 38, size, size));
            Embers.Add(spark);
            Fx.Fly(spark, Embers, rnd.Next(-30, 30), -rnd.Next(60, 120), (uint)rnd.Next(900, 1700), 1.0);
            await Task.Delay(rnd.Next(150, 320));
        }
    }

    // «Продовжити» має сенс, якщо гра вже йде або є збереження
    private void UpdateButtons()
    {
        var canContinue = GameState.Current.Started || GameState.HasSave;
        ContinueButton.IsEnabled = canContinue;
        ContinueButton.Opacity = canContinue ? 1 : 0.5;
        LoadButton.IsEnabled = GameState.HasSave;
        LoadButton.Opacity = GameState.HasSave ? 1 : 0.5;
        SaveButton.IsEnabled = GameState.Current.Started;
        SaveButton.Opacity = GameState.Current.Started ? 1 : 0.5;
    }

    private async void OnContinueClicked(object? sender, EventArgs e)
    {
        if (!GameState.Current.Started && !GameState.Current.Load()) return;
        await Shell.Current.GoToAsync(nameof(HomePage));
    }

    private async void OnNewGameClicked(object? sender, EventArgs e)
    {
        if (GameState.Current.Started)
        {
            var sure = await Dialog.ShowAsync("НОВА ГРА?", "Поточний прогрес буде втрачено, якщо ти його не зберіг.", "ПОЧАТИ", "СКАСУВАТИ");
            if (!sure) return;
        }
        GameState.Current.NewGame();
        await Shell.Current.GoToAsync(nameof(HomePage));
    }

    private async void OnLoadClicked(object? sender, EventArgs e)
    {
        var ok = GameState.Current.Load();
        UpdateButtons();
        await UiHelpers.ToastAsync(ToastLabel, ok ? "ГРУ ЗАВАНТАЖЕНО" : "НЕ ВДАЛОСЯ ЗАВАНТАЖИТИ");
    }

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        GameState.Current.Save();
        UpdateButtons();
        await UiHelpers.ToastAsync(ToastLabel, "ГРУ ЗБЕРЕЖЕНО");
    }

    private async void OnSettingsClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(SettingsPage));

    private void OnExitClicked(object? sender, EventArgs e) => Application.Current?.Quit();
}
