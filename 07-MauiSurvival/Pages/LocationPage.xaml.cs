using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

// Id локації приходить з параметра маршруту: LocationPage?id=forest
[QueryProperty(nameof(LocationId), "id")]
public partial class LocationPage : ContentPage
{
    /// <summary>Один рядок на екрані: дія, її кнопка і напис зі станом.</summary>
    private sealed record Row(GatherAction Action, Button Button, Label Info);

    private Place _location = Catalog.Locations[0];
    private readonly List<Row> _rows = new();

    public LocationPage()
    {
        InitializeComponent();
    }

    public string LocationId
    {
        set
        {
            _location = Catalog.LocationById(value);
            Build();
        }
    }

    private CancellationTokenSource? _anim;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += Refresh;
        GameState.Current.Message += OnMessage;
        Refresh();
        _ = Fx.EnterAsync(this);

        _anim = new CancellationTokenSource();
        _ = WorkerAsync(_anim.Token);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameState.Current.Changed -= Refresh;
        GameState.Current.Message -= OnMessage;
        _anim?.Cancel();
    }

    /// <summary>Поки йде збір, герой «працює»: змінюються кадри спрайта і він підстрибує в такт.</summary>
    private async Task WorkerAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (GameState.Current.Task is not null)
            {
                WorkerImage.Source = "hero_work.png";
                await WorkerImage.TranslateTo(0, -8, 140, Easing.CubicOut);
                WorkerImage.Source = "hero.png";
                await WorkerImage.TranslateTo(0, 0, 140, Easing.CubicIn);
            }
            else
            {
                WorkerImage.Source = "hero.png";
                await Task.Delay(120);
            }
        }
    }

    /// <summary>Отримали ресурси чи новий рівень: над екраном злітає яскравий напис.</summary>
    private void OnMessage(string text)
    {
        if (!(text.StartsWith("+") || text.StartsWith("НОВИЙ"))) return;
        var color = text.StartsWith("+") ? Color.FromArgb("#A7F070") : Color.FromArgb("#FFCD75");
        var x = Math.Max(20, Width / 2 - 120);
        _ = Fx.FloatTextAsync(Overlay, text, color, x, 300, 110, 1500);
    }

    private void Build()
    {
        LocationIcon.Source = _location.Icon;
        TitleLabel.Text = _location.Name;
        DescriptionLabel.Text = _location.Description;

        ActionsLayout.Clear();
        _rows.Clear();
        foreach (var action in _location.Actions)
        {
            var item = Catalog.ItemById(action.ItemId);

            var info = new Label { FontSize = 9, LineHeight = 1.6, LineBreakMode = LineBreakMode.WordWrap };
            var index = Array.IndexOf(_location.Actions, action);
            // Кнопка відкриває окрему сцену дії: там і стартує збір
            var button = UiHelpers.PixelButton("ПОЧАТИ", async (_, _) => await Shell.Current.GoToAsync($"{nameof(ScenePage)}?place={_location.Id}&action={index}"), "GreenButton", 11);
            button.HeightRequest = 52;
            button.WidthRequest = 140;

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(56), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 14 };
            grid.Add(new Image { Source = item.Icon, WidthRequest = 48, HeightRequest = 48, VerticalOptions = LayoutOptions.Center });
            grid.Add(new VerticalStackLayout
            {
                Spacing = 6,
                VerticalOptions = LayoutOptions.Center,
                Children = { new Label { Text = action.Name, FontSize = 12, TextColor = UiHelpers.Res("Gold") }, info },
            }, 1);
            grid.Add(button, 2);

            ActionsLayout.Add(UiHelpers.Card(grid, padding: 14));
            _rows.Add(new Row(action, button, info));
        }
    }

    /// <summary>Оновлює підписи і доступність кнопок: активна дія, нестача енергії, зайнятий герой.</summary>
    private void Refresh()
    {
        var g = GameState.Current;
        foreach (var (action, button, info) in _rows)
        {
            var bonus = g.Tools.Any(t => Catalog.ToolById(t).BoostedItems.Contains(action.ItemId)) ? 1 : 0;
            var item = Catalog.ItemById(action.ItemId);
            var yield = $"+{action.MinYield + bonus}-{action.MaxYield + bonus} {item.Name}";
            var active = g.Task?.Action == action;

            string status =
                active ? "ЙДЕ ЗБІР..." :
                g.IsBusy ? "ГЕРОЙ ЗАЙНЯТИЙ" :
                g.Energy < action.EnergyCost ? "НЕ ВИСТАЧАЄ ЕНЕРГІЇ" : "ГОТОВО";

            info.Text = $"ЕНЕРГІЯ -{action.EnergyCost}  ·  {action.Seconds} С  ·  {yield}\n{status}";
            button.Text = active ? "ДИВИТИСЬ" : "ПОЧАТИ";
            button.IsEnabled = g.CanStart(action) || active;
            button.Opacity = button.IsEnabled || active ? 1 : 0.5;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
