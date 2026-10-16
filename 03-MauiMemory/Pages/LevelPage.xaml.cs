using MemoryGame.Models;

namespace MemoryGame.Pages;

public partial class LevelPage : ContentPage
{
    // Який малюнок показувати біля кожного рівня
    private static readonly string[] LevelPictures = { "deco_ghost.png", "deco_apple.png", "deco_gem.png" };

    public LevelPage()
    {
        InitializeComponent();
    }

    // Перебудовуємо картки щоразу, коли екран з'являється, щоб показувати свіжі рекорди.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        LevelsLayout.Clear();

        foreach (var level in LevelConfig.All)
            LevelsLayout.Add(CreateLevelCard(level));
    }

    /// <summary>Картка рівня: малюнок зліва, назва, опис і рекорд справа. Реагує на наведення мишки.</summary>
    private View CreateLevelCard(LevelConfig level)
    {
        var bestMoves = Preferences.Get(level.BestMovesKey, 0);
        var panel = (Color)Application.Current!.Resources["Panel"];
        var ink = (Color)Application.Current!.Resources["Ink"];
        var gold = (Color)Application.Current!.Resources["Gold"];

        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(80), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 16 };
        grid.Add(new Image { Source = LevelPictures[(level.Number - 1) % LevelPictures.Length], HeightRequest = 64, WidthRequest = 64, VerticalOptions = LayoutOptions.Center });
        grid.Add(new VerticalStackLayout
        {
            Spacing = 7,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = $"{level.Number}. {level.Name}", FontSize = 15, TextColor = gold },
                new Label { Text = level.Description, FontSize = 9, LineBreakMode = LineBreakMode.WordWrap },
                new Label
                {
                    Text = bestMoves > 0 ? $"РЕКОРД: {bestMoves} ХОДІВ" : "РЕКОРДУ ЩЕ НЕМАЄ",
                    FontSize = 9,
                    TextColor = bestMoves > 0 ? (Color)Application.Current!.Resources["Green"] : Color.FromArgb("#8D91A0"),
                },
            },
        }, 1);

        var card = new Border
        {
            Content = grid,
            BackgroundColor = panel,
            Stroke = ink,
            StrokeThickness = 4,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
            Padding = new Thickness(16, 14),
        };

        // Клік по картці запускає гру на цьому рівні
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Shell.Current.GoToAsync($"{nameof(GamePage)}?level={level.Number}");
        card.GestureRecognizers.Add(tap);

        // Підсвічування при наведенні мишки (на телефоні просто не спрацьовує)
        var hover = new PointerGestureRecognizer();
        hover.PointerEntered += (_, _) => card.BackgroundColor = Color.FromArgb("#454F73");
        hover.PointerExited += (_, _) => card.BackgroundColor = panel;
        card.GestureRecognizers.Add(hover);

        SemanticProperties.SetDescription(card, $"Рівень {level.Number}: {level.Name}. {level.Description}");
        return card;
    }

    // ".." означає "повернутися на попередній екран"
    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
