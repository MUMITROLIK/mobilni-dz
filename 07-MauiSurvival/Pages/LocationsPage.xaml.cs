using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

public partial class LocationsPage : ContentPage
{
    private int _builtForLevel = -1;

    public LocationsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += OnChanged;
        _ = Services.Fx.EnterAsync(this);
        Build();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameState.Current.Changed -= OnChanged;
    }

    // Перебудовуємо список лише коли змінився рівень (нова локація могла відкритися)
    private void OnChanged()
    {
        if (GameState.Current.Level != _builtForLevel) Build();
    }

    private void Build()
    {
        _builtForLevel = GameState.Current.Level;
        LocationsLayout.Clear();
        foreach (var location in Catalog.Locations) LocationsLayout.Add(CreateCard(location));
    }

    /// <summary>Картка локації: малюнок, назва, опис, які ресурси там є. Закриті локації затемнені.</summary>
    private View CreateCard(Place location)
    {
        var unlocked = GameState.Current.Level >= location.RequiredLevel;

        var resources = new HorizontalStackLayout { Spacing = 6 };
        foreach (var itemId in location.Actions.Select(a => a.ItemId).Distinct())
            resources.Add(new Image { Source = Catalog.ItemById(itemId).Icon, WidthRequest = 28, HeightRequest = 28 });

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(80), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 14 };
        grid.Add(new Image { Source = location.Icon, WidthRequest = 60, HeightRequest = 60, VerticalOptions = LayoutOptions.Center });
        grid.Add(new VerticalStackLayout
        {
            Spacing = 7,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = location.Name, FontSize = 15, TextColor = UiHelpers.Res("Gold") },
                new Label { Text = location.Description, FontSize = 9, LineHeight = 1.6, LineBreakMode = LineBreakMode.WordWrap },
                new Label
                {
                    Text = unlocked ? "ВІДКРИТО" : $"ПОТРІБЕН РІВЕНЬ {location.RequiredLevel}",
                    FontSize = 9,
                    TextColor = unlocked ? UiHelpers.Res("Green") : Color.FromArgb("#EF7D57"),
                },
                resources,
            },
        }, 1);

        // Кнопка переходу: зелена, якщо локація відкрита, і сіра «закрито», якщо ні
        var go = UiHelpers.PixelButton(unlocked ? "ЙТИ" : "ЗАКРИТО", async (_, _) =>
        {
            if (!unlocked) await UiHelpers.ToastAsync(ToastLabel, $"ЛОКАЦІЯ ВІДКРИЄТЬСЯ НА РІВНІ {location.RequiredLevel}");
            else await Shell.Current.GoToAsync($"{nameof(LocationPage)}?id={location.Id}");
        }, unlocked ? "GreenButton" : "PixelButton", 11);
        go.WidthRequest = 120;
        go.HeightRequest = 52;
        go.VerticalOptions = LayoutOptions.Center;
        if (!unlocked) go.Opacity = 0.5;
        grid.Add(go, 2);

        var card = UiHelpers.Card(grid, unlocked ? UiHelpers.Res("Panel") : Color.FromArgb("#262A3A"), 16);
        if (!unlocked) foreach (var image in grid.Children.OfType<Image>()) image.Opacity = 0.45;

        SemanticProperties.SetDescription(card, $"Локація {location.Name}. {(unlocked ? "Відкрито" : "Закрито")}");
        return card;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
