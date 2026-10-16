using Survival.Services;

namespace Survival.Controls;

public partial class HudBar : ContentView
{
    private readonly PixelBar _xp = new(30, 8);
    private readonly PixelBar _health = new();
    private readonly PixelBar _hunger = new();
    private readonly PixelBar _thirst = new();
    private readonly PixelBar _energy = new();
    private readonly PixelBar _task = new(30, 10);
    private readonly PixelBar[] _bars;
    private IDispatcherTimer? _frameTimer;
    private CancellationTokenSource? _messageTimer;
    private CancellationTokenSource? _alarm;

    /// <summary>Чи показувати рядок поточного завдання (на сцені дії він зайвий, бо там своя смуга).</summary>
    public bool ShowTask { get; set; } = true;

    public HudBar()
    {
        InitializeComponent();
        _bars = new[] { _xp, _health, _hunger, _thirst, _energy, _task };

        XpRow.Content = _xp;
        TaskBarRow.Content = _task;
        BarsGrid.Add(_health, 1, 0);
        BarsGrid.Add(_hunger, 1, 1);
        BarsGrid.Add(_thirst, 1, 2);
        BarsGrid.Add(_energy, 1, 3);

        // Підписуємось на зміни гри, поки панель на екрані, і відписуємось, коли вона зникла
        Loaded += (_, _) =>
        {
            GameState.Current.Changed += Refresh;
            GameState.Current.Message += ShowMessage;
            GameState.Current.AutoSaved += OnAutoSaved;
            Refresh();

            // Окремий таймер ~60 разів на секунду плавно «доганяє» смугами цільові значення,
            // навіть коли сама гра рахує час рідше. Так індикатори рухаються без ривків.
            _frameTimer = Dispatcher.CreateTimer();
            _frameTimer.Interval = TimeSpan.FromMilliseconds(16);
            _frameTimer.Tick += (_, _) => { foreach (var bar in _bars) bar.Step(); };
            _frameTimer.Start();

            _alarm = new CancellationTokenSource();
            _ = AlarmAsync(HealthIcon, () => GameState.Current.Health < 25, _alarm.Token);
            _ = AlarmAsync(HungerIcon, () => GameState.Current.Hunger < 25, _alarm.Token);
            _ = AlarmAsync(ThirstIcon, () => GameState.Current.Thirst < 25, _alarm.Token);
            _ = AlarmAsync(EnergyIcon, () => GameState.Current.Energy < 15, _alarm.Token);
        };
        Unloaded += (_, _) =>
        {
            GameState.Current.Changed -= Refresh;
            GameState.Current.Message -= ShowMessage;
            GameState.Current.AutoSaved -= OnAutoSaved;
            _frameTimer?.Stop();
            _alarm?.Cancel();
        };
    }

    private void Refresh()
    {
        var g = GameState.Current;

        LevelLabel.Text = $"РІВЕНЬ {g.Level}   ДОСВІД {g.Xp}/{g.XpToNext}";
        DayLabel.Text = $"ДЕНЬ {g.Day}";
        _xp.Set(g.Xp, g.XpToNext, Color.FromArgb("#41A6F6"));

        _health.Set(g.Health, 100, Color.FromArgb("#B13E53"));
        _hunger.Set(g.Hunger, 100, Color.FromArgb("#EF7D57"));
        _thirst.Set(g.Thirst, 100, Color.FromArgb("#41A6F6"));
        _energy.Set(g.Energy, 100, Color.FromArgb("#FFCD75"));
        HealthLabel.Text = ((int)Math.Ceiling(g.Health)).ToString();
        HungerLabel.Text = ((int)Math.Ceiling(g.Hunger)).ToString();
        ThirstLabel.Text = ((int)Math.Ceiling(g.Thirst)).ToString();
        EnergyLabel.Text = ((int)Math.Ceiling(g.Energy)).ToString();

        if (g.Task is { } task)
        {
            ShowTaskBox();
            TaskLabel.Text = $"{task.Action.Name}  {Math.Max(0, (int)Math.Ceiling(task.Remaining))} С";
            _task.Set(task.Progress, 1, Color.FromArgb("#38B764"));
        }
        else if (g.Fight is { } fight)
        {
            ShowTaskBox();
            TaskLabel.Text = $"БІЙ: {fight.Enemy.Name}  {(int)Math.Ceiling(fight.EnemyHp)}/{fight.Enemy.Hp}";
            _task.Set(fight.EnemyHp, fight.Enemy.Hp, Color.FromArgb("#B13E53"));
        }
        else if (g.IsSleeping)
        {
            ShowTaskBox();
            TaskLabel.Text = $"СПИМО...  {(int)Math.Ceiling(g.SleepRemaining)} С";
            _task.Set(1 - g.SleepRemaining / GameState.SleepSeconds, 1, Color.FromArgb("#41A6F6"));
        }
        else if (TaskBox.IsVisible)
        {
            _task.Set(1, 1, Color.FromArgb("#38B764"));   // доїжджає до кінця, потім панель ховається
            HideTaskBox();
        }
    }

    /// <summary>Автозбереження: напис «ЗБЕРЕЖЕНО» на мить спалахує і гасне.</summary>
    private async void OnAutoSaved()
    {
        await SaveLabel.FadeTo(1, 150);
        await Task.Delay(900);
        await SaveLabel.FadeTo(0, 500);
    }

    // Панель завдання плавно висувається і ховається
    private void ShowTaskBox()
    {
        if (!ShowTask || TaskBox.IsVisible) return;
        TaskBox.Opacity = 0;
        TaskBox.IsVisible = true;
        _ = TaskBox.FadeTo(1, 200);
    }

    private async void HideTaskBox()
    {
        await TaskBox.FadeTo(0, 350);
        var g = GameState.Current;
        if (g.Task is null && g.Fight is null && !g.IsSleeping) TaskBox.IsVisible = false;
    }

    /// <summary>Коротке повідомлення виринає, трохи висить і плавно зникає. Нова характеристика «підстрибує».</summary>
    private async void ShowMessage(string text)
    {
        MessageLabel.Text = text;
        MessageLabel.Opacity = 0;
        MessageLabel.TranslationY = 8;
        MessageLabel.IsVisible = true;
        _ = MessageLabel.FadeTo(1, 160);
        _ = MessageLabel.TranslateTo(0, 0, 200, Easing.CubicOut);

        if (text.StartsWith("НОВИЙ РІВЕНЬ")) _ = Fx.PopAsync(LevelLabel, 1.35);

        _messageTimer?.Cancel();
        _messageTimer = new CancellationTokenSource();
        try
        {
            await Task.Delay(2600, _messageTimer.Token);
            await MessageLabel.FadeTo(0, 300);
            MessageLabel.IsVisible = false;
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Коли показник небезпечно низький, його іконка пульсує і привертає увагу.</summary>
    private static async Task AlarmAsync(Image icon, Func<bool> isLow, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (isLow())
            {
                await icon.ScaleTo(1.3, 260, Easing.SinInOut);
                await icon.ScaleTo(1.0, 260, Easing.SinInOut);
            }
            else
            {
                await Task.Delay(400);
            }
        }
    }
}
