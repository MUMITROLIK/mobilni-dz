using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

/// <summary>Прокачка героя: створення і покращення інструментів та зброї, вкладання очок навичок.</summary>
public partial class UpgradePage : ContentPage
{
    private string _signature = "";   // перебудовуємо екран, лише коли змінились ресурси, рівні чи очки

    public UpgradePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += OnChanged;
        _ = Fx.EnterAsync(this);
        _signature = "";
        OnChanged();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameState.Current.Changed -= OnChanged;
    }

    private void OnChanged()
    {
        var g = GameState.Current;
        var signature = string.Join(",", Catalog.Items.Select(i => g.Count(i.Id))) + "|" +
                        string.Join(",", Catalog.Tools.Select(t => g.LevelOf(t.Id))) + "|" + g.SkillPoints + g.Strength + g.Endurance;
        if (signature == _signature) return;
        _signature = signature;
        Build();
    }

    private void Build()
    {
        var g = GameState.Current;

        // ----- Інструменти і зброя -----
        ToolsLayout.Clear();
        foreach (var tool in Catalog.Tools)
        {
            var level = g.LevelOf(tool.Id);
            var maxed = level >= Tool.MaxLevel;
            var cost = string.Join(" + ", g.NextCost(tool).Select(c => $"{c.Value} {Catalog.ItemById(c.Key).Name}"));

            var title = level == 0 ? tool.Name : $"{tool.Name}  РІВЕНЬ {level}";
            var status = maxed ? "МАКСИМАЛЬНИЙ РІВЕНЬ" : $"{(level == 0 ? "СТВОРИТИ" : "ПОКРАЩИТИ")}: {cost}";

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(56), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            grid.Add(new Image { Source = tool.Icon, WidthRequest = 44, HeightRequest = 44, VerticalOptions = LayoutOptions.Center, Opacity = level > 0 ? 1 : 0.5 });
            grid.Add(new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = title, FontSize = 12, TextColor = UiHelpers.Res("Gold") },
                    new Label { Text = tool.Description, FontSize = 8 },
                    new Label { Text = status, FontSize = 8, TextColor = maxed ? UiHelpers.Res("Green") : Color.FromArgb("#94B0C2"), LineBreakMode = LineBreakMode.WordWrap },
                },
            }, 1);

            var button = UiHelpers.PixelButton(maxed ? "МАКС" : level == 0 ? "СТВОРИТИ" : "ПОКРАЩИТИ", async (_, _) =>
            {
                if (!g.Upgrade(tool)) await UiHelpers.ToastAsync(ToastLabel, "НЕ ВИСТАЧАЄ РЕСУРСІВ");
            }, "GreenButton", 10);
            button.IsEnabled = g.CanUpgrade(tool);
            button.Opacity = button.IsEnabled ? 1 : 0.4;
            button.Padding = new Thickness(14, 10);
            grid.Add(button, 2);

            ToolsLayout.Add(UiHelpers.Card(grid, padding: 12));
        }

        // ----- Навички героя -----
        PointsLabel.Text = $"ОЧОК НАВИЧОК: {g.SkillPoints}  ·  +1 ЗА КОЖЕН НОВИЙ РІВЕНЬ  ·  УДАР У БОЮ: {g.Damage}";
        SkillsLayout.Clear();
        SkillsLayout.Add(SkillCard("СИЛА", "stat_health.png", g.Strength, "+2 до шкоди за очко", 0));
        SkillsLayout.Add(SkillCard("ВИТРИВАЛІСТЬ", "stat_energy.png", g.Endurance, $"-5% витрат енергії за очко (зараз -{Math.Min(50, g.Endurance * 5)}%)", 1));
    }

    private Border SkillCard(string name, string icon, int points, string description, int skill)
    {
        var g = GameState.Current;
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(56), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        grid.Add(new Image { Source = icon, WidthRequest = 36, HeightRequest = 36, VerticalOptions = LayoutOptions.Center });
        grid.Add(new VerticalStackLayout
        {
            Spacing = 5,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = $"{name}  {points}", FontSize = 12, TextColor = UiHelpers.Res("Gold") },
                new Label { Text = description, FontSize = 8, LineBreakMode = LineBreakMode.WordWrap },
            },
        }, 1);

        var plus = UiHelpers.PixelButton("+1", async (_, _) =>
        {
            if (!g.SpendSkillPoint(skill)) await UiHelpers.ToastAsync(ToastLabel, "НЕМАЄ ОЧОК. ЗАРОБИ НОВИЙ РІВЕНЬ");
        }, "GreenButton", 12);
        plus.IsEnabled = g.SkillPoints > 0 && !(skill == 1 && g.Endurance >= 10);
        plus.Opacity = plus.IsEnabled ? 1 : 0.4;
        plus.Padding = new Thickness(18, 10);
        grid.Add(plus, 2);

        return UiHelpers.Card(grid, padding: 12);
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
