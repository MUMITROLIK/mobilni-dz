using MemoryGame.Models;

namespace MemoryGame.Pages;

// Номер рівня приходить із параметра маршруту: GamePage?level=2
[QueryProperty(nameof(LevelNumber), "level")]
public partial class GamePage : ContentPage
{
    private const string BackImage = "card_back.png";
    private const int FaceCount = 18;   // скільки різних малюнків намальовано (face_00 ... face_17)

    /// <summary>Одна картка на полі: який на ній малюнок і в якому вона стані.</summary>
    private sealed class Card
    {
        public int Face;
        public bool IsOpen;
        public bool IsMatched;
        public ImageButton Button = null!;
        public string FaceImage => $"face_{Face:D2}.png";
    }

    private LevelConfig _level = LevelConfig.All[0];
    private readonly List<Card> _cards = new();
    private readonly IDispatcherTimer _timer;
    private Card? _first;       // перша відкрита картка з поточної пари
    private bool _busy;        // поки анімація йде, нові кліки ігноруємо
    private int _moves;
    private int _pairs;
    private int _seconds;

    public GamePage()
    {
        InitializeComponent();

        // Секундомір: тік раз на секунду
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => { _seconds++; UpdateStats(); };
    }

    // Shell кладе значення параметра сюди як рядок, після цього починаємо гру
    public string LevelNumber
    {
        set
        {
            if (int.TryParse(value, out var n)) _level = LevelConfig.Get(n);
            StartNewGame();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _timer.Stop();
    }

    /// <summary>Скидає стан, перемішує колоду і будує поле карток.</summary>
    private void StartNewGame()
    {
        _timer.Stop();
        _first = null;
        _busy = false;
        _moves = _pairs = _seconds = 0;
        LevelLabel.Text = _level.Name;

        // Беремо потрібну кількість різних малюнків і розкладаємо їх у колоду потрібну кількість разів
        var faces = Enumerable.Range(0, FaceCount).OrderBy(_ => Random.Shared.Next()).Take(_level.Kinds).ToList();
        var deck = faces.SelectMany(f => Enumerable.Repeat(f, _level.CopiesPerKind)).ToArray();
        Random.Shared.Shuffle(deck);

        BuildBoard(deck);
        UpdateStats();

        var best = Preferences.Get(_level.BestMovesKey, 0);
        RecordLabel.Text = best > 0 ? $"РЕКОРД: {best} ходів" : "РЕКОРДУ ЩЕ НЕМАЄ";
    }

    private void BuildBoard(int[] deck)
    {
        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();
        _cards.Clear();

        // На великому полі картки менші, щоб усе вмістилося на екран
        var size = _level.Columns >= 6 ? 72 : 98;
        for (var c = 0; c < _level.Columns; c++) BoardGrid.ColumnDefinitions.Add(new ColumnDefinition(size));
        for (var r = 0; r < _level.Rows; r++) BoardGrid.RowDefinitions.Add(new RowDefinition(size));

        for (var i = 0; i < deck.Length; i++)
        {
            var card = new Card { Face = deck[i] };
            card.Button = new ImageButton
            {
                Source = BackImage,
                WidthRequest = size,
                HeightRequest = size,
                Padding = 0,
                BackgroundColor = Colors.Transparent,
                Aspect = Aspect.AspectFit,
            };
            card.Button.AutomationId = $"card_{i}";   // зручно для автотестів
            card.Button.Clicked += async (_, _) => await OnCardClickedAsync(card);
            _cards.Add(card);
            BoardGrid.Add(card.Button, i % _level.Columns, i / _level.Columns);
        }
    }

    private async Task OnCardClickedAsync(Card card)
    {
        if (_busy || card.IsOpen || card.IsMatched) return;
        if (_moves == 0 && _first is null) _timer.Start();   // час іде з першого кліка

        card.IsOpen = true;

        // Перша картка пари: просто показуємо її
        if (_first is null)
        {
            _first = card;
            await FlipAsync(card, open: true);
            return;
        }

        // Друга картка: рахуємо хід і порівнюємо
        _busy = true;
        var first = _first;
        _first = null;
        _moves++;
        UpdateStats();
        await FlipAsync(card, open: true);

        if (first.Face == card.Face)
        {
            first.IsMatched = card.IsMatched = true;
            _pairs++;
            UpdateStats();
            await Task.WhenAll(PulseAsync(first), PulseAsync(card));
            if (_pairs == _level.PairCount) await OnWinAsync();
        }
        else
        {
            await Task.Delay(700);   // даємо гравцю запам'ятати малюнки
            await Task.WhenAll(FlipAsync(first, open: false), FlipAsync(card, open: false));
            first.IsOpen = card.IsOpen = false;
        }

        _busy = false;
    }

    /// <summary>Анімація перевороту: картка стискається до лінії, змінюється малюнок і розправляється.</summary>
    private static async Task FlipAsync(Card card, bool open)
    {
        await card.Button.ScaleXTo(0, 80, Easing.CubicIn);
        card.Button.Source = open ? card.FaceImage : BackImage;
        await card.Button.ScaleXTo(1, 80, Easing.CubicOut);
    }

    private static async Task PulseAsync(Card card)
    {
        await card.Button.ScaleTo(1.12, 90);
        await card.Button.ScaleTo(1.0, 90);
    }

    private async Task OnWinAsync()
    {
        _timer.Stop();

        // Рекорд: менше ходів - краще
        var best = Preferences.Get(_level.BestMovesKey, 0);
        var isRecord = best == 0 || _moves < best;
        if (isRecord)
        {
            Preferences.Set(_level.BestMovesKey, _moves);
            Preferences.Set(_level.BestSecondsKey, _seconds);
        }

        var text = $"Ходів: {_moves}\nЧас: {FormatTime(_seconds)}" + (isRecord ? "\n\nНовий рекорд!" : "");
        var again = await Dialog.ShowAsync("ПЕРЕМОГА!", text, "ЩЕ РАЗ", "МЕНЮ");
        if (again) StartNewGame();
        else await Shell.Current.GoToAsync("//MenuPage");
    }

    private void UpdateStats()
    {
        MovesLabel.Text = $"ХОДИ: {_moves}";
        PairsLabel.Text = $"ПАРИ: {_pairs}/{_level.PairCount}";
        TimeLabel.Text = $"ЧАС: {FormatTime(_seconds)}";
    }

    private static string FormatTime(int seconds) => $"{seconds / 60}:{seconds % 60:D2}";

    private void OnRestartClicked(object? sender, EventArgs e) => StartNewGame();

    // "//MenuPage" - повернутися на кореневий екран, минаючи вибір рівня
    private async void OnMenuClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("//MenuPage");
}
