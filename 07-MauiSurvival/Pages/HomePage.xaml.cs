using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private CancellationTokenSource? _anim;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += Refresh;
        Refresh();
        _ = Fx.EnterAsync(this);

        _anim = new CancellationTokenSource();
        _ = IdleAsync(_anim.Token);
        _ = BlinkAsync(_anim.Token);
        _ = SnoreAsync(_anim.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameState.Current.Changed -= Refresh;
        _anim?.Cancel();
    }

    /// <summary>Герой «дихає»: плавно підіймається і опускається, поки ми в таборі.</summary>
    private async Task IdleAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.WhenAll(HeroImage.TranslateTo(0, -5, 650, Easing.SinInOut), WizardImage.TranslateTo(0, -4, 650, Easing.SinInOut));
            await Task.WhenAll(HeroImage.TranslateTo(0, 0, 650, Easing.SinInOut), WizardImage.TranslateTo(0, 0, 650, Easing.SinInOut));
        }
    }

    /// <summary>Час від часу герой кліпає: на мить змінюється кадр спрайта.</summary>
    private async Task BlinkAsync(CancellationToken token)
    {
        var rnd = new Random();
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(rnd.Next(2200, 4200));
            if (GameState.Current.IsSleeping) { HeroImage.Source = "hero_blink.png"; continue; }   // уві сні очі закриті
            HeroImage.Source = "hero_blink.png";
            await Task.Delay(130);
            HeroImage.Source = "hero.png";
        }
    }

    /// <summary>Поки герой спить, над ним злітають літери Z.</summary>
    private async Task SnoreAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (GameState.Current.IsSleeping)
            {
                _ = Fx.FloatTextAsync(HeroOverlay, "Z", Color.FromArgb("#73EFF7"), 60, 6, 50, 1400);
                await Task.Delay(700);
            }
            else
            {
                if (HeroImage.Source is FileImageSource { File: "hero_blink.png" }) HeroImage.Source = "hero.png";
                await Task.Delay(250);
            }
        }
    }

    /// <summary>Підказка про стан героя і ряд створених інструментів.</summary>
    private void Refresh()
    {
        var g = GameState.Current;
        StatusLabel.Text =
            g.Hunger < 25 ? "Ти дуже голодний! Поїж, поки не втратив здоров'я." :
            g.Energy < 20 ? "Сил майже нема. Відпочинь або поспи." :
            g.IsSleeping ? "Ти спиш і відновлюєш сили..." :
            g.Task is not null ? "Герой зайнятий збором ресурсів." :
            "Усе добре. Вирушай на розвідку.";

        ToolsRow.Clear();
        foreach (var tool in Catalog.Tools.Where(t => g.Tools.Contains(t.Id)))
            ToolsRow.Add(new Image { Source = tool.Icon, WidthRequest = 36, HeightRequest = 36 });
    }

    private async void OnExploreClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(LocationsPage));
    private async void OnInventoryClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(InventoryPage));

    private async void OnSleepClicked(object? sender, EventArgs e)
    {
        var g = GameState.Current;
        if (g.IsSleeping || (!g.IsBusy && g.Energy < 99))
            await Shell.Current.GoToAsync($"{nameof(ScenePage)}?mode=sleep");   // окрема сцена сну
        else
            await UiHelpers.ToastAsync(ToastLabel, g.IsBusy ? "ЗАРАЗ ЗАЙНЯТИЙ" : "ТИ ВЖЕ ВІДПОЧИВ");
    }

    /// <summary>Їмо найменш поживну їжу з наявної, щоб не марнувати цінну.</summary>
    private async void OnEatClicked(object? sender, EventArgs e)
    {
        var g = GameState.Current;
        var food = Catalog.Items.Where(i => i.FoodValue > 0 && g.Count(i.Id) > 0).OrderBy(i => i.FoodValue).FirstOrDefault();

        if (food is null) await UiHelpers.ToastAsync(ToastLabel, "НЕМАЄ ЇЖІ. ЗБЕРИ ЯГОДИ АБО ЛОВИ РИБУ");
        else if (!g.Eat(food.Id)) await UiHelpers.ToastAsync(ToastLabel, "ТИ ВЖЕ СИТИЙ");
        else await UiHelpers.ToastAsync(ToastLabel, $"З'ЇДЕНО: {food.Name}");
    }

    // Повернення в головне меню; "//MenuPage" очищає стек екранів
    private async void OnMenuClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
}
