namespace MemoryGame.Pages;

public partial class MenuPage : ContentPage
{
    private bool _animating;

    public MenuPage()
    {
        InitializeComponent();
    }

    // Анімації запускаємо, коли екран з'являється, і зупиняємо, коли ми його покинули
    protected override void OnAppearing()
    {
        base.OnAppearing();
        _animating = true;
        _ = BobAsync(DecoMoon, 10, 1800);
        _ = BobAsync(DecoStar, 8, 1400);
        _ = BobAsync(DecoGhost, 12, 2200);
        _ = BobAsync(DecoHeart, 9, 1600);
        _ = FlipDemoCardsAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _animating = false;
    }

    /// <summary>Плавно гойдає картинку вгору-вниз, поки меню на екрані.</summary>
    private async Task BobAsync(VisualElement view, double height, uint period)
    {
        while (_animating)
        {
            await view.TranslateTo(0, -height, period, Easing.SinInOut);
            await view.TranslateTo(0, 0, period, Easing.SinInOut);
        }
    }

    /// <summary>Кожні півтори секунди випадкова картка в ряду перевертається на випадковий малюнок або назад.</summary>
    private async Task FlipDemoCardsAsync()
    {
        while (_animating)
        {
            await Task.Delay(1300);
            if (!_animating) break;

            var card = (Image)DemoRow.Children[Random.Shared.Next(DemoRow.Children.Count)];
            var showBack = card.Source is FileImageSource { File: not "card_back.png" };
            await card.ScaleXTo(0, 90, Easing.CubicIn);
            card.Source = showBack ? "card_back.png" : $"face_{Random.Shared.Next(18):D2}.png";
            await card.ScaleXTo(1, 90, Easing.CubicOut);
        }
    }

    // Перехід на другий екран: ім'я маршруту зареєстроване в AppShell.xaml.cs
    private async void OnPlayClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(LevelPage));

    private async void OnRulesClicked(object? sender, EventArgs e) =>
        await Dialog.ShowAsync("ПРАВИЛА",
            "Відкривай по дві картки.\nОднакові залишаються відкритими, різні закриваються.\n\nЗнайди всі пари за якомога менше ходів!",
            "ЗРОЗУМІЛО");

    private void OnExitClicked(object? sender, EventArgs e) => Application.Current?.Quit();
}
