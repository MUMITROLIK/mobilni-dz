using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

public partial class InventoryPage : ContentPage
{
    private string _signature = "";   // «відбиток» вмісту інвентаря: перебудовуємо екран, лише коли він змінився

    public InventoryPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += OnChanged;
        _ = Services.Fx.EnterAsync(this);
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
        var signature = string.Join(",", Catalog.Items.Select(i => g.Count(i.Id))) + "|" + string.Join(",", g.Tools.Order()) + "|" + (int)g.Hunger / 10;
        if (signature == _signature) return;
        _signature = signature;
        Build();
    }

    private void Build()
    {
        var g = GameState.Current;

        // ----- Ресурси -----
        ItemsLayout.Clear();
        foreach (var item in Catalog.Items)
        {
            var count = g.Count(item.Id);
            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(56), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            grid.Add(new Image { Source = item.Icon, WidthRequest = 40, HeightRequest = 40, VerticalOptions = LayoutOptions.Center, Opacity = count > 0 ? 1 : 0.35 });

            var text = new VerticalStackLayout { Spacing = 5, VerticalOptions = LayoutOptions.Center };
            text.Add(new Label { Text = item.Name, FontSize = 11 });
            if (item.FoodValue > 0) text.Add(new Label { Text = $"ЇЖА: СИТІСТЬ +{item.FoodValue}", FontSize = 8, TextColor = UiHelpers.Res("Green") });
            grid.Add(text, 1);

            var right = new HorizontalStackLayout { Spacing = 10, VerticalOptions = LayoutOptions.Center };
            right.Add(new Label { Text = $"x{count}", FontSize = 14, TextColor = UiHelpers.Res("Gold"), VerticalOptions = LayoutOptions.Center });
            if (item.FoodValue > 0)
            {
                var eat = UiHelpers.PixelButton("З'ЇСТИ", async (_, _) =>
                {
                    if (!g.Eat(item.Id)) await UiHelpers.ToastAsync(ToastLabel, "ТИ ВЖЕ СИТИЙ");
                }, "PixelButton", 9);
                eat.IsEnabled = count > 0;
                eat.Opacity = count > 0 ? 1 : 0.4;
                eat.Padding = new Thickness(12, 8);
                right.Add(eat);
            }
            grid.Add(right, 2);

            ItemsLayout.Add(UiHelpers.Card(grid, count > 0 ? UiHelpers.Res("Panel") : Color.FromArgb("#262A3A"), 10));
        }

        // ----- Інструменти -----
        ToolsLayout.Clear();
        foreach (var tool in Catalog.Tools)
        {
            var owned = g.Tools.Contains(tool.Id);
            var cost = string.Join(" + ", tool.Cost.Select(c => $"{c.Value} {Catalog.ItemById(c.Key).Name}"));

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(56), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
            grid.Add(new Image { Source = tool.Icon, WidthRequest = 44, HeightRequest = 44, VerticalOptions = LayoutOptions.Center });
            grid.Add(new VerticalStackLayout
            {
                Spacing = 5,
                VerticalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label { Text = tool.Name, FontSize = 12, TextColor = UiHelpers.Res("Gold") },
                    new Label { Text = tool.Description, FontSize = 8 },
                    new Label { Text = owned ? "ВЖЕ Є" : $"ПОТРІБНО: {cost}", FontSize = 8, TextColor = owned ? UiHelpers.Res("Green") : Color.FromArgb("#94B0C2"), LineBreakMode = LineBreakMode.WordWrap },
                },
            }, 1);

            var craft = UiHelpers.PixelButton(owned ? "Є" : "СТВОРИТИ", async (_, _) =>
            {
                if (!g.Craft(tool)) await UiHelpers.ToastAsync(ToastLabel, "НЕ ВИСТАЧАЄ РЕСУРСІВ");
            }, "GreenButton", 10);
            craft.IsEnabled = g.CanCraft(tool);
            craft.Opacity = craft.IsEnabled ? 1 : 0.4;
            craft.Padding = new Thickness(14, 10);
            grid.Add(craft, 2);

            ToolsLayout.Add(UiHelpers.Card(grid, padding: 12));
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
