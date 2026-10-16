using Survival.Controls;
using Survival.Models;
using Survival.Services;

namespace Survival.Pages;

/// <summary>
/// Сцена дії: окремий екран, де видно, що саме робить герой. Він замахується інструментом,
/// ціль трясеться, летять щепки, а в кінці показується нагорода. Такий самий екран є для сну.
/// Параметри: ScenePage?place=forest&amp;action=0  або  ScenePage?mode=sleep
/// </summary>
[QueryProperty(nameof(PlaceId), "place")]
[QueryProperty(nameof(ActionIndex), "action")]
[QueryProperty(nameof(Mode), "mode")]
public partial class ScenePage : ContentPage
{
    // ----- Параметри маршруту -----
    public string PlaceId { get; set; } = "forest";
    public string ActionIndex { get; set; } = "0";
    public string Mode { get; set; } = "";

    // ----- Вигляд сцени залежить від ресурсу (що б'ємо) і від локації (небо, земля, атмосфера) -----
    private sealed record TargetStyle(string Image, string? Tool, string[] Chips, string Verb, bool Falls);
    private sealed record PlaceStyle(string[] Sky, string Ground, string GroundDark, string? SkyDecor, string Ambient, string[]? Tiles, string Critter, string CritterImage);

    private static readonly Dictionary<string, TargetStyle> Targets = new()
    {
        ["wood"] = new("big_loc_forest.png", "big_tool_axe.png", new[] { "#A05B3B", "#FFCD75", "#38B764" }, "РУБАЄМО ДЕРЕВО", true),
        ["stone"] = new("big_res_stone.png", "big_tool_pickaxe.png", new[] { "#94B0C2", "#566C86", "#F4F4F4" }, "ДОБУВАЄМО КАМІНЬ", false),
        ["berry"] = new("big_res_berry.png", null, new[] { "#B13E53", "#38B764", "#F4F4F4" }, "ЗБИРАЄМО ЯГОДИ", false),
        ["mushroom"] = new("big_res_mushroom.png", null, new[] { "#B13E53", "#F4F4F4", "#FFCD75" }, "ШУКАЄМО ГРИБИ", false),
        ["cactus"] = new("big_res_cactus.png", "big_tool_axe.png", new[] { "#38B764", "#257179", "#A7F070" }, "РІЖЕМО КАКТУС", false),
        ["fish"] = new("big_res_fish.png", "big_tool_rod.png", new[] { "#41A6F6", "#73EFF7", "#F4F4F4" }, "ЛОВИМО РИБУ", false),
        ["ice"] = new("big_res_ice.png", "big_tool_pickaxe.png", new[] { "#73EFF7", "#F4F4F4", "#41A6F6" }, "КОЛЕМО ЛІД", false),
        ["ore"] = new("big_res_ore.png", "big_tool_pickaxe.png", new[] { "#94B0C2", "#FFCD75", "#566C86" }, "ДОБУВАЄМО РУДУ", false),
        ["crystal"] = new("big_res_crystal.png", "big_tool_pickaxe.png", new[] { "#B55088", "#F4F4F4", "#5D275D" }, "ШУКАЄМО КРИСТАЛИ", false),
    };

    private static readonly Dictionary<string, PlaceStyle> Places = new()
    {
        ["forest"] = new(new[] { "#1A2A3A", "#1F3A44", "#24505A", "#2D6A63" }, "#2F8F57", "#257A4A", "icon_sleep.png", "fireflies", null, "slime", "k_slime.png"),
        ["desert"] = new(new[] { "#7A3B4F", "#A24B4B", "#D06A4A", "#EFA05A" }, "#E0B36A", "#C99A52", "loc_desert.png", "dust", new[] { "k_floor0.png", "k_floor1.png", "k_floor2.png", "k_floor3.png" }, "spider", "k_spider.png"),
        ["tundra"] = new(new[] { "#2F4A73", "#4B6F9C", "#7EA0C4", "#B8D4E8" }, "#E8F1F7", "#C7D8E6", "loc_tundra.png", "snow", null, "ghost", "k_ghost.png"),
        ["cave"] = new(new[] { "#0D0D16", "#14131F", "#1C1B2A", "#262538" }, "#3A3A4E", "#2C2C3C", null, "drips", new[] { "k_brick0.png", "k_brick1.png", "k_brick2.png" }, "bat", "k_bat.png"),
    };

    private bool IsSleepScene => Mode == "sleep";

    private Place _place = Catalog.Locations[0];
    private GatherAction _action = Catalog.Locations[0].Actions[0];
    private TargetStyle _target = Targets["wood"];
    private PlaceStyle _style = Places["forest"];
    private readonly PixelBar _progress = new(30, 14);
    private IDispatcherTimer? _frameTimer;
    private CancellationTokenSource? _loops;
    private bool _active;
    private bool _finished;

    public ScenePage()
    {
        InitializeComponent();
        ProgressHolder.Content = _progress;
        Stage.SizeChanged += (_, _) => { if (_style.Tiles is not null && Stage.Width > 0) { GroundTexture.Clear(); BuildTiles(); } };
    }

    // ================= Життєвий цикл =================

    protected override void OnAppearing()
    {
        base.OnAppearing();
        GameState.Current.Changed += OnChanged;

        _frameTimer = Dispatcher.CreateTimer();
        _frameTimer.Interval = TimeSpan.FromMilliseconds(16);
        _frameTimer.Tick += (_, _) => _progress.Step();
        _frameTimer.Start();

        _ = Fx.EnterAsync(this);
        BeginScene();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        GameState.Current.Changed -= OnChanged;
        _frameTimer?.Stop();
        _loops?.Cancel();
    }

    /// <summary>Готує сцену: кольори, ціль, інструмент і запускає дію в грі.</summary>
    private void BeginScene()
    {
        var g = GameState.Current;
        _finished = false;
        _loops?.Cancel();
        _loops = new CancellationTokenSource();

        if (IsSleepScene)
        {
            _place = Catalog.Locations[0];
            _style = Places["forest"];
            if (!g.IsSleeping && !g.Sleep()) { _ = LeaveAsync(); return; }
            SceneTitle.Text = "ВІДПОЧИВАЄМО";
        }
        else
        {
            _place = Catalog.LocationById(PlaceId);
            _action = _place.Actions[Math.Clamp(int.Parse(ActionIndex), 0, _place.Actions.Length - 1)];
            _target = Targets[_action.ItemId];
            _style = Places[_place.Id];

            // Якщо ми повернулися на сцену під час уже запущеного збору, просто продовжуємо
            if (g.Task?.Action != _action && !g.StartTask(_place, _action)) { _ = LeaveAsync(); return; }
            SceneTitle.Text = _target.Verb;
        }

        _active = true;
        ApplyStyle();
        ResetVisuals();

        var token = _loops.Token;
        if (!IsSleepScene) _ = CritterAsync(token);
        _ = AmbientAsync(token);
        _ = IsSleepScene ? SleepLoopAsync(token) : WorkLoopAsync(token);
        OnChanged();
    }

    private async Task LeaveAsync() => await Shell.Current.GoToAsync("..");

    // ================= Вигляд =================

    private void ApplyStyle()
    {
        var sky = new[] { Sky0, Sky1, Sky2, Sky3 };
        for (var i = 0; i < 4; i++) sky[i].Color = Color.FromArgb(_style.Sky[i]);
        Ground.Color = Color.FromArgb(_style.Ground);
        GroundEdge.Color = Color.FromArgb(_style.GroundDark);
        SkyDecor.Source = _style.SkyDecor;
        SkyDecor.IsVisible = _style.SkyDecor is not null;

        // Текстура землі: або плитки Kenney (пустеля, печера), або розкидані темніші «пікселі»
        GroundTexture.Clear();
        if (_style.Tiles is not null) { BuildTiles(); goto textureDone; }
        var rnd = new Random(_place.Id.GetHashCode());
        for (var i = 0; i < 46; i++)
        {
            var size = rnd.Next(6, 14);
            var tuft = new BoxView { WidthRequest = size, HeightRequest = size / 2, Color = Color.FromArgb(_style.GroundDark), Opacity = 0.7 };
            AbsoluteLayout.SetLayoutFlags(tuft, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(tuft, new Rect(rnd.NextDouble(), 0.12 + rnd.NextDouble() * 0.85, size, size / 2));
            GroundTexture.Add(tuft);
        }

        textureDone:
        if (IsSleepScene)
        {
            TargetImage.Source = "logo_fire.png";
            TargetImage.WidthRequest = TargetImage.HeightRequest = 128;
            ToolImage.IsVisible = false;
            HeroImage.Source = "hero_blink_big.png";
        }
        else
        {
            TargetImage.Source = _target.Image;
            TargetImage.WidthRequest = TargetImage.HeightRequest = 150;
            ToolImage.Source = _target.Tool;
            ToolImage.IsVisible = _target.Tool is not null;
            HeroImage.Source = "hero_big.png";
        }
    }

    private void ResetVisuals()
    {
        ResultPanel.IsVisible = false;
        WorkButtons.IsVisible = true;
        DoneButtons.IsVisible = false;
        TargetImage.Opacity = 1;
        TargetImage.Rotation = 0;
        TargetImage.Scale = 1;
        TargetImage.ScaleX = TargetImage.ScaleY = 1;
        TargetImage.TranslationX = TargetImage.TranslationY = 0;
        TargetImage.AnchorX = 0.5;
        TargetImage.AnchorY = 1;
        HeroCell.TranslationX = HeroCell.TranslationY = 0;
        HeroCell.Rotation = 0;
        ToolImage.Rotation = 0;
        ToolImage.IsVisible = false;   // на панелі результату інструмент зайвий
        FxLayer.Clear();
        _progress.Set(0, 1, Color.FromArgb("#38B764"));
    }

    // ================= Прогрес і завершення =================

    private void OnChanged()
    {
        if (!_active) return;
        var g = GameState.Current;

        if (IsSleepScene)
        {
            ProgressLabel.Text = $"СПИМО...  {(int)Math.Ceiling(g.SleepRemaining)} С";
            _progress.Set(1 - g.SleepRemaining / GameState.SleepSeconds, 1, Color.FromArgb("#41A6F6"));
            if (!g.IsSleeping) _ = FinishAsync();
        }
        else if (g.Task is { } task && task.Action == _action)
        {
            ProgressLabel.Text = $"{_target.Verb}...  {Math.Max(0, (int)Math.Ceiling(task.Remaining))} С";
            _progress.Set(task.Progress, 1, Color.FromArgb("#38B764"));
        }
        else
        {
            _ = FinishAsync();   // завдання зникло: або закінчилось, або скасоване
        }
    }

    private async Task FinishAsync()
    {
        if (_finished) return;
        _finished = true;
        _active = false;
        _loops?.Cancel();

        var g = GameState.Current;
        ProgressLabel.Text = "";
        _progress.Set(1, 1, Color.FromArgb("#38B764"));
        HeroCell.TranslationX = HeroCell.TranslationY = 0;
        ToolImage.Rotation = 0;

        if (IsSleepScene)
        {
            ResultTitle.Text = "ДОБРОГО РАНКУ!";
            ResultIcon.Source = "stat_energy.png";
            ResultAmount.Text = $"ЕНЕРГІЯ: {(int)g.Energy}";
            ResultInfo.Text = "Сили відновлено. Можна знову рушати в дорогу.";
            HeroImage.Source = "hero_big.png";
            AgainButton.Text = "СПАТИ ЩЕ";
            AgainButton.IsEnabled = g.Energy < 99;
        }
        else
        {
            if (g.LastGain is not { } gain || gain.ItemId != _action.ItemId)
            {
                await LeaveAsync();    // збір скасовано: нагороди немає
                return;
            }

            // Ціль зникає: дерево падає, решта зменшується і розсипається
            if (_target.Falls)
            {
                await Task.WhenAll(TargetImage.RotateTo(78, 620, Easing.CubicIn), TargetImage.TranslateTo(40, 0, 620, Easing.CubicIn));
                SpawnChips(14, 1.0);
                await TargetImage.FadeTo(0, 260);
            }
            else
            {
                SpawnChips(12, 1.0);
                await Task.WhenAll(TargetImage.ScaleTo(0.2, 360, Easing.CubicIn), TargetImage.FadeTo(0, 360));
            }

            var item = Catalog.ItemById(gain.ItemId);
            ResultTitle.Text = "ГОТОВО!";
            ResultIcon.Source = item.Icon;
            ResultAmount.Text = $"+{gain.Amount} {item.Name}";
            ResultInfo.Text = $"ДОСВІД +{gain.Xp}" + (gain.LeveledUp ? $"\nНОВИЙ РІВЕНЬ: {g.Level}!" : "");
            AgainButton.Text = "ЩЕ РАЗ";
            AgainButton.IsEnabled = g.Energy >= _action.EnergyCost;
            AgainButton.Opacity = AgainButton.IsEnabled ? 1 : 0.5;
        }

        // Панель результату виринає з пружинним ефектом
        ResultChest.IsVisible = !IsSleepScene;
        ResultChest.Source = "k_chest_closed.png";
        ResultChest.Rotation = 0;
        ResultIcon.Opacity = IsSleepScene ? 1 : 0;
        ResultIcon.TranslationY = 0;
        ResultIcon.VerticalOptions = IsSleepScene ? LayoutOptions.Center : LayoutOptions.Start;
        ResultPanel.Scale = 0.6;
        ResultPanel.Opacity = 0;
        ResultPanel.IsVisible = true;
        _ = ResultPanel.FadeTo(1, 160);
        await ResultPanel.ScaleTo(1.08, 170, Easing.CubicOut);
        await ResultPanel.ScaleTo(1, 130, Easing.CubicIn);

        // Сундук трясеться, відкривається, і з нього вистрибує нагорода
        if (!IsSleepScene)
        {
            for (var i = 0; i < 3; i++) { await ResultChest.RotateTo(-7, 60); await ResultChest.RotateTo(7, 60); }
            await ResultChest.RotateTo(0, 60);
            ResultChest.Source = "k_chest_open.png";
            ResultIcon.TranslationY = 80;   // стартує всередині сундука
            ResultIcon.Scale = 0.4;
            _ = ResultIcon.FadeTo(1, 140);
            _ = ResultIcon.ScaleTo(1.25, 260, Easing.CubicOut);
            await ResultIcon.TranslateTo(0, -6, 300, Easing.CubicOut);
            await ResultIcon.ScaleTo(1, 160, Easing.CubicIn);
        }

        WorkButtons.IsVisible = false;
        DoneButtons.IsVisible = true;
    }

    // ================= Анімація роботи =================

    /// <summary>Цикл «замах - удар - відскок», поки триває завдання.</summary>
    private async Task WorkLoopAsync(CancellationToken token)
    {
        await Task.Delay(250);
        while (!token.IsCancellationRequested)
        {
            // Замах: руки вгору, інструмент відхиляється назад
            HeroImage.Source = "hero_work_big.png";
            await Task.WhenAll(ToolImage.RotateTo(-62, 230, Easing.CubicOut), HeroCell.TranslateTo(-8, -14, 230, Easing.CubicOut));
            if (token.IsCancellationRequested) break;

            // Удар: різкий рух уперед
            await Task.WhenAll(ToolImage.RotateTo(38, 110, Easing.CubicIn), HeroCell.TranslateTo(34, 0, 110, Easing.CubicIn));
            _ = HitAsync();

            // Відскок
            HeroImage.Source = "hero_big.png";
            await Task.WhenAll(ToolImage.RotateTo(0, 220, Easing.CubicOut), HeroCell.TranslateTo(0, 0, 280, Easing.CubicOut));
            await Task.Delay(120);
        }
    }

    /// <summary>Момент влучання: ціль трясеться і приплющується, спалах, щепки.</summary>
    private async Task HitAsync()
    {
        _ = Flash.FadeTo(0.28, 35);
        SpawnChips(_target.Falls ? 8 : 6, 0.8);

        await Task.WhenAll(TargetImage.TranslateTo(11, 0, 45, Easing.CubicOut), TargetImage.ScaleXTo(1.08, 45), TargetImage.ScaleYTo(0.93, 45));
        _ = Flash.FadeTo(0, 120);
        await Task.WhenAll(TargetImage.TranslateTo(-8, 0, 60, Easing.SinInOut), TargetImage.ScaleXTo(0.97, 60), TargetImage.ScaleYTo(1.03, 60));
        await Task.WhenAll(TargetImage.TranslateTo(0, 0, 90, Easing.CubicOut), TargetImage.ScaleXTo(1, 90), TargetImage.ScaleYTo(1, 90));
    }

    /// <summary>
    /// Розкидає уламки: кожен летить по параболі (як під дією гравітації) і гасне.
    /// Рух рахується формулою, а не покадровими малюнками, тому він плавний на будь-якій частоті оновлення.
    /// </summary>
    private void SpawnChips(int count, double power)
    {
        var rnd = Random.Shared;
        var originX = TargetCell.X + TargetCell.Width / 2;
        var originY = TargetCell.Y + TargetCell.Height * 0.55;

        for (var i = 0; i < count; i++)
        {
            var size = rnd.Next(6, 13);
            var chip = new BoxView
            {
                WidthRequest = size,
                HeightRequest = size,
                Color = Color.FromArgb(_target.Chips[rnd.Next(_target.Chips.Length)]),
                InputTransparent = true,
            };
            AbsoluteLayout.SetLayoutBounds(chip, new Rect(originX - size / 2, originY, size, size));
            FxLayer.Add(chip);

            double vx = (rnd.NextDouble() * 2 - 1) * 220 * power;
            double vy = (160 + rnd.NextDouble() * 240) * power;
            const double gravity = 900;
            var duration = 0.55 + rnd.NextDouble() * 0.45;

            var animation = new Animation(t =>
            {
                var s = t * duration;
                chip.TranslationX = vx * s;
                chip.TranslationY = -(vy * s - 0.5 * gravity * s * s);
                chip.Rotation = t * 540;
                chip.Opacity = 1 - t * t;
            });
            animation.Commit(this, $"chip{chip.GetHashCode()}", 16, (uint)(duration * 1000), Easing.Linear, (_, _) => FxLayer.Remove(chip));
        }
    }

    // ================= Анімація сну =================

    private async Task SleepLoopAsync(CancellationToken token)
    {
        // Герой лежить біля вогнища: поверніть спрайт набік
        await Task.WhenAll(HeroCell.RotateTo(90, 450, Easing.CubicOut), HeroCell.TranslateTo(-30, 52, 450, Easing.CubicOut));

        while (!token.IsCancellationRequested)
        {
            _ = FloatZAsync();
            await Task.WhenAll(HeroCell.ScaleTo(1.03, 900, Easing.SinInOut));
            await HeroCell.ScaleTo(1.0, 900, Easing.SinInOut);
        }
    }

    private async Task FloatZAsync()
    {
        var x = HeroCell.X + HeroCell.Width / 2 + 40;
        var y = HeroCell.Y + HeroCell.Height * 0.35;
        await Fx.FloatTextAsync(FxLayer, "Z", Color.FromArgb("#73EFF7"), x, y, 70, 1500);
    }


    // ================= Плитки землі й мешканці місця (спрайти Kenney, CC0) =================

    /// <summary>Вимощує землю плитками 64x64 (плитки вибираються випадково з набору локації).</summary>
    private void BuildTiles()
    {
        if (_style.Tiles is null || Stage.Width <= 0) return;
        const double size = 64;
        var rnd = new Random(_place.Id.GetHashCode());
        var groundHeight = Stage.Height * 0.38;
        for (var row = 0; row * size < groundHeight; row++)
        {
            for (var col = 0; col * size < Stage.Width; col++)
            {
                var tile = new Image { Source = _style.Tiles[rnd.Next(_style.Tiles.Length)], WidthRequest = size, HeightRequest = size, Aspect = Aspect.Fill, InputTransparent = true };
                AbsoluteLayout.SetLayoutBounds(tile, new Rect(col * size, row * size, size, size));
                GroundTexture.Add(tile);
            }
        }
    }

    /// <summary>Мешканець локації (слиз, павук, привид, кажан) бродить і літає на задньому плані.</summary>
    private async Task CritterAsync(CancellationToken token)
    {
        var critter = new Image { Source = _style.CritterImage, WidthRequest = 72, HeightRequest = 72, InputTransparent = true, Opacity = _style.Critter == "ghost" ? 0.8 : 1 };
        AbsoluteLayout.SetLayoutBounds(critter, new Rect(0, 0, 72, 72));
        FxLayer.Add(critter);

        await Task.Delay(150);
        var rnd = new Random();
        var w = Stage.Width > 0 ? Stage.Width : 800;
        var h = Stage.Height > 0 ? Stage.Height : 400;
        var baseY = _style.Critter is "ghost" ? h * 0.20 : _style.Critter is "bat" ? h * 0.24 : h * 0.74;
        var x = w * 0.52;
        critter.TranslationX = x;
        critter.TranslationY = baseY;

        while (!token.IsCancellationRequested)
        {
            var nx = Math.Clamp(x + rnd.Next(-230, 230), 30, w - 110);
            critter.ScaleX = nx < x ? -1 : 1;

            switch (_style.Critter)
            {
                case "slime":   // стрибками по землі
                    await critter.TranslateTo((x + nx) / 2, baseY - 36, 240, Easing.CubicOut);
                    await critter.TranslateTo(nx, baseY, 240, Easing.CubicIn);
                    await Task.Delay(rnd.Next(500, 1300), token).ContinueWith(_ => { });
                    break;
                case "spider":  // повзе
                    await critter.TranslateTo(nx, baseY + rnd.Next(-8, 8), (uint)(Math.Abs(nx - x) * 4 + 300), Easing.Linear);
                    await Task.Delay(rnd.Next(400, 1100), token).ContinueWith(_ => { });
                    break;
                case "ghost":   // повільно пливе
                    await critter.TranslateTo(nx, baseY + rnd.Next(-24, 24), 2800, Easing.SinInOut);
                    break;
                default:        // кажан: ширяє і змахує крилами (розтягується по висоті)
                    _ = critter.ScaleYTo(0.8, 160).ContinueWith(_ => { });
                    await critter.TranslateTo(nx, baseY + rnd.Next(-20, 50), 1600, Easing.SinInOut);
                    await critter.ScaleYTo(1, 120);
                    break;
            }
            x = nx;
        }
    }
    // ================= Атмосфера: світлячки, пил, сніг, краплі =================

    private async Task AmbientAsync(CancellationToken token)
    {
        var rnd = new Random();
        var kind = IsSleepScene ? "fireflies" : _style.Ambient;
        // Вогнище при сні: іскри
        if (IsSleepScene) _ = EmbersAsync(token);

        while (!token.IsCancellationRequested)
        {
            var w = Stage.Width > 0 ? Stage.Width : 800;
            var h = Stage.Height > 0 ? Stage.Height : 400;
            BoxView p;
            double fromX, fromY, dx, dy, ms;

            switch (kind)
            {
                case "snow":
                    p = Particle("#F4F4F4", rnd.Next(4, 8)); fromX = rnd.NextDouble() * w; fromY = -10;
                    dx = rnd.Next(-60, 60); dy = h * 0.8 + 20; ms = rnd.Next(2800, 5200); break;
                case "dust":
                    p = Particle("#F4D9A0", rnd.Next(3, 6)); fromX = -10; fromY = rnd.NextDouble() * h * 0.7;
                    dx = w + 30; dy = rnd.Next(-40, 40); ms = rnd.Next(2600, 4600); break;
                case "drips":
                    p = Particle("#73EFF7", 5); fromX = rnd.NextDouble() * w; fromY = 0;
                    dx = 0; dy = h * 0.6; ms = rnd.Next(900, 1500); break;
                default: // світлячки
                    p = Particle("#FFCD75", rnd.Next(4, 7)); fromX = rnd.NextDouble() * w; fromY = h * (0.25 + rnd.NextDouble() * 0.5);
                    dx = rnd.Next(-90, 90); dy = rnd.Next(-70, 70); ms = rnd.Next(2200, 4200); break;
            }

            AbsoluteLayout.SetLayoutBounds(p, new Rect(fromX, fromY, p.WidthRequest, p.HeightRequest));
            FxLayer.Add(p);
            // Одна анімація на частинку: позиція і прозорість рахуються разом (світлячки ще й погойдуються)
            Fx.Fly(p, FxLayer, dx, dy, (uint)ms, 0.85, kind == "fireflies" ? 10 : 0);
            await Task.Delay(rnd.Next(170, 380));   // не більше кількох десятків частинок одночасно: це береже процесор
        }
    }

    private static BoxView Particle(string color, int size) =>
        new() { WidthRequest = size, HeightRequest = size, Color = Color.FromArgb(color), Opacity = 0, InputTransparent = true };

    private async Task EmbersAsync(CancellationToken token)
    {
        var rnd = new Random();
        string[] colors = { "#FFCD75", "#EF7D57", "#F4F4F4" };
        while (!token.IsCancellationRequested)
        {
            var size = rnd.Next(4, 8);
            var spark = Particle(colors[rnd.Next(colors.Length)], size);
            AbsoluteLayout.SetLayoutBounds(spark, new Rect(TargetCell.X + TargetCell.Width / 2 + rnd.Next(-16, 16), TargetCell.Y + TargetCell.Height * 0.3, size, size));
            FxLayer.Add(spark);
            Fx.Fly(spark, FxLayer, rnd.Next(-30, 30), -rnd.Next(60, 130), 1400, 1.0);
            await Task.Delay(rnd.Next(140, 300));
        }
    }

    // ================= Кнопки =================

    private void OnCancelClicked(object? sender, EventArgs e)
    {
        _active = false;
        _loops?.Cancel();
        if (IsSleepScene) GameState.Current.WakeUp(); else GameState.Current.CancelTask();
        _ = LeaveAsync();
    }

    private void OnAgainClicked(object? sender, EventArgs e)
    {
        if (!AgainButton.IsEnabled) return;
        BeginScene();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await LeaveAsync();
}
