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
        var signature = string.Join(",", Catalog.Items.Select(i => g.Count(i.Id))) + "|" + (int)g.Hunger / 10 + "|" + (int)g.Thirst / 10;
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
            if (item.DrinkValue > 0) text.Add(new Label { Text = $"ПИТВО: СПРАГА -{item.DrinkValue}", FontSize = 8, TextColor = Color.FromArgb("#41A6F6") });
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
            if (item.DrinkValue > 0)
            {
                var drink = UiHelpers.PixelButton("ВИПИТИ", async (_, _) =>
                {
                    if (!g.Drink(item.Id)) await UiHelpers.ToastAsync(ToastLabel, "ТИ ВЖЕ НАПИВСЯ");
                }, "PixelButton", 9);
                drink.IsEnabled = count > 0;
                drink.Opacity = count > 0 ? 1 : 0.4;
                drink.Padding = new Thickness(12, 8);
                right.Add(drink);
            }
            grid.Add(right, 2);

            ItemsLayout.Add(UiHelpers.Card(grid, count > 0 ? UiHelpers.Res("Panel") : Color.FromArgb("#262A3A"), 10));
        }

    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
