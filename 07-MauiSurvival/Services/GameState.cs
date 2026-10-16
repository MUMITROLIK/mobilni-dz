using System.Text.Json;
using Survival.Models;

namespace Survival.Services;

/// <summary>Завдання, яке виконується у фоні (збір ресурсу): що робимо і скільки секунд лишилось.</summary>
public class ActiveTask
{
    public required GatherAction Action { get; init; }
    public required Place Place { get; init; }
    public double Remaining { get; set; }
    public double Progress => 1 - Remaining / Action.Seconds;
}

/// <summary>Поточний бій: ворог, його здоров'я і таймери ударів.</summary>
public class ActiveFight
{
    public required Enemy Enemy { get; init; }
    public required Place Place { get; init; }
    public double EnemyHp { get; set; }
    public double HeroTimer { get; set; }
    public double EnemyTimer { get; set; }
}

/// <summary>
/// Стан гри: характеристики, інвентар, навички, рівень, поточна дія і бій. Один спільний об'єкт на всі екрани,
/// тому, коли ми переходимо між екранами, гра не губить прогрес, а збір ресурсів іде далі.
/// Ігровий час рухає таймер: кожні 100 мс викликається Tick із справжнім інтервалом часу.
/// </summary>
public class GameState
{
    public static GameState Current { get; } = new();

    public const double SleepSeconds = 10;
    private const double SecondsPerDay = 120;
    private const double AutosaveEvery = 15;          // секунд між автозбереженнями
    private const double HeroAttackEvery = 0.9;        // секунд між ударами героя в бою
    private const double EnemyAttackEvery = 1.3;       // секунд між ударами ворога
    public const int FightEnergyCost = 10;

    // ----- Характеристики (0..100) -----
    public double Health { get; private set; } = 100;
    public double Hunger { get; private set; } = 100;    // ситість: 100 - ситий, 0 - голодний
    public double Thirst { get; private set; } = 100;    // 100 - напився, 0 - мучить спрага
    public double Energy { get; private set; } = 100;

    // ----- Прогрес -----
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => Level * 25;
    public double PlaySeconds { get; private set; }
    public int Day => 1 + (int)(PlaySeconds / SecondsPerDay);

    // ----- Навички (очки дає кожен новий рівень) -----
    public int SkillPoints { get; private set; }
    public int Strength { get; private set; }       // +2 до удару за очко
    public int Endurance { get; private set; }      // -5% витрат енергії за очко (до -50%)

    // ----- Інвентар і інструменти -----
    public Dictionary<string, int> Items { get; private set; } = new();
    public Dictionary<string, int> ToolLevels { get; private set; } = new();

    // ----- Поточна дія -----
    public ActiveTask? Task { get; private set; }
    public ActiveFight? Fight { get; private set; }
    public double SleepRemaining { get; private set; }
    public bool IsSleeping => SleepRemaining > 0;
    public bool IsBusy => Task is not null || IsSleeping || Fight is not null;

    /// <summary>Чи розпочата гра (щоб кнопка «Продовжити» мала сенс).</summary>
    public bool Started { get; private set; }

    /// <summary>Щось змінилось: екрани перемальовують смуги і списки.</summary>
    public event Action? Changed;

    /// <summary>Повідомлення для гравця (наприклад, «+3 ДЕРЕВО»).</summary>
    public event Action<string>? Message;

    /// <summary>Удар у бою: хто вдарив (герой чи ворог), скільки шкоди і чи був критичний удар.</summary>
    public event Action<bool, int, bool>? FightHit;

    /// <summary>Відбулося автозбереження (для маленького значка в інтерфейсі).</summary>
    public event Action? AutoSaved;

    private IDispatcherTimer? _timer;
    private double _sinceSave;

    // ================= Таймер =================

    public void StartClock()
    {
        if (_timer is not null) return;
        _timer = Application.Current!.Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);

        // Міряємо справжній час між тіками: якщо інтерфейс зайнятий і тік запізнився,
        // гра все одно йде зі своєю швидкістю (10 секунд збору = 10 справжніх секунд).
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var last = clock.Elapsed.TotalSeconds;
        _timer.Tick += (_, _) =>
        {
            var now = clock.Elapsed.TotalSeconds;
            var dt = Math.Min(now - last, 0.5);   // обмеження, щоб після паузи гра не «перестрибнула»
            last = now;
            Tick(dt);
        };
        _timer.Start();
    }

    /// <summary>Один крок ігрового часу: голод, спрага, енергія, виконання завдання чи бою, автозбереження.</summary>
    public void Tick(double dt)
    {
        if (!Started) return;
        PlaySeconds += dt;

        // Голод і спрага ростуть завжди, а під час сну повільніше
        Hunger = Math.Max(0, Hunger - dt / (IsSleeping ? 12.0 : 6.0));
        Thirst = Math.Max(0, Thirst - dt / (IsSleeping ? 16.0 : 8.0));

        // Енергія відновлюється: швидко уві сні, повільно у спокої, під час роботи й бою не відновлюється
        if (IsSleeping) Energy = Math.Min(100, Energy + dt * 6);
        else if (Task is null && Fight is null) Energy = Math.Min(100, Energy + dt * 0.35);

        // Без їжі або води здоров'я падає, а коли все гаразд, повільно відновлюється
        if (Hunger <= 0 || Thirst <= 0) Health = Math.Max(0, Health - dt * 0.8);
        else if (Hunger > 60 && Thirst > 50 && Energy > 30 && Fight is null) Health = Math.Min(100, Health + dt * 0.3);

        if (IsSleeping) SleepRemaining = Math.Max(0, SleepRemaining - dt);

        if (Task is not null)
        {
            Task.Remaining -= dt;
            if (Task.Remaining <= 0) FinishTask();
        }

        if (Fight is not null) FightStep(dt);

        if (Health <= 0) Faint();

        // Автозбереження: раз на 15 секунд ігрового часу
        _sinceSave += dt;
        if (_sinceSave >= AutosaveEvery) AutoSave();

        Changed?.Invoke();
    }

    private void AutoSave()
    {
        _sinceSave = 0;
        try
        {
            Save();
            AutoSaved?.Invoke();
        }
        catch (IOException)
        {
            // не вдалося записати файл - спробуємо наступного разу
        }
    }

    // ================= Нова гра =================

    public void NewGame()
    {
        Health = Hunger = Thirst = Energy = 100;
        Level = 1;
        Xp = 0;
        PlaySeconds = 0;
        SkillPoints = Strength = Endurance = 0;
        Items = new();
        ToolLevels = new();
        Task = null;
        Fight = null;
        SleepRemaining = 0;
        Started = true;
        _sinceSave = 0;
        Changed?.Invoke();
    }

    // ================= Збір ресурсів =================

    /// <summary>Витрата енергії з урахуванням навички «Витривалість» (знижка 5% за очко, не більше 50%).</summary>
    public int CostOf(int baseCost) => Math.Max(1, (int)Math.Ceiling(baseCost * (1 - Math.Min(0.5, Endurance * 0.05))));

    public bool CanStart(GatherAction action) => !IsBusy && Energy >= CostOf(action.EnergyCost);

    /// <summary>Запускає збір на 10 секунд: витрачає енергію одразу, а ресурси видає в кінці.</summary>
    public bool StartTask(Place location, GatherAction action)
    {
        if (!CanStart(action)) return false;
        Energy -= CostOf(action.EnergyCost);
        Task = new ActiveTask { Action = action, Place = location, Remaining = action.Seconds };
        Changed?.Invoke();
        return true;
    }

    /// <summary>Підсумок останнього збору: показується на екрані сцени після завершення.</summary>
    public record Gain(string ItemId, int Amount, int Xp, bool LeveledUp);
    public Gain? LastGain { get; private set; }

    /// <summary>Бонус до видобутку: сума рівнів усіх інструментів, що підсилюють цей ресурс.</summary>
    public int YieldBonus(string itemId) =>
        Catalog.Tools.Where(t => t.BoostedItems.Contains(itemId)).Sum(t => LevelOf(t.Id));

    private void FinishTask()
    {
        var task = Task!;
        Task = null;

        var amount = Random.Shared.Next(task.Action.MinYield, task.Action.MaxYield + 1) + YieldBonus(task.Action.ItemId);
        Add(task.Action.ItemId, amount);

        var levelBefore = Level;
        Message?.Invoke($"+{amount} {Catalog.ItemById(task.Action.ItemId).Name}");
        GainXp(task.Action.Xp);
        LastGain = new Gain(task.Action.ItemId, amount, task.Action.Xp, Level > levelBefore);
        AutoSave();
    }

    private void GainXp(int xp)
    {
        Xp += xp;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            SkillPoints++;
            Health = 100;
            Message?.Invoke($"НОВИЙ РІВЕНЬ: {Level}! +1 ОЧКО НАВИЧОК");
        }
    }

    /// <summary>Перервати збір: енергія вже витрачена і не повертається, ресурсів не буде.</summary>
    public void CancelTask()
    {
        if (Task is null) return;
        Task = null;
        Message?.Invoke("ЗБІР ПЕРЕРВАНО");
        Changed?.Invoke();
    }

    // ================= Сон, їжа, вода =================

    public void WakeUp()
    {
        if (!IsSleeping) return;
        SleepRemaining = 0;
        Changed?.Invoke();
    }

    public bool Sleep()
    {
        if (IsBusy || Energy >= 99) return false;
        SleepRemaining = SleepSeconds;
        Changed?.Invoke();
        return true;
    }

    /// <summary>З'їсти одну одиницю їжі: ситість росте, предмет зникає з інвентаря.</summary>
    public bool Eat(string itemId)
    {
        var item = Catalog.ItemById(itemId);
        if (item.FoodValue <= 0 || Count(itemId) == 0 || Hunger >= 99) return false;

        Items[itemId]--;
        Hunger = Math.Min(100, Hunger + item.FoodValue);
        Message?.Invoke($"СИТІСТЬ +{item.FoodValue}");
        Changed?.Invoke();
        return true;
    }

    /// <summary>Випити воду (або розтопити лід, зрізати кактус): спрага слабшає.</summary>
    public bool Drink(string itemId)
    {
        var item = Catalog.ItemById(itemId);
        if (item.DrinkValue <= 0 || Count(itemId) == 0 || Thirst >= 99) return false;

        Items[itemId]--;
        Thirst = Math.Min(100, Thirst + item.DrinkValue);
        Message?.Invoke($"СПРАГА -{item.DrinkValue}");
        Changed?.Invoke();
        return true;
    }

    // ================= Інструменти, зброя і прокачування =================

    public int LevelOf(string toolId) => ToolLevels.TryGetValue(toolId, out var l) ? l : 0;
    public bool HasTool(string toolId) => LevelOf(toolId) > 0;

    /// <summary>Вартість наступного рівня (створення, якщо ще нема, або покращення).</summary>
    public Dictionary<string, int> NextCost(Tool tool) => tool.CostOfLevel(LevelOf(tool.Id) + 1);

    public bool CanUpgrade(Tool tool) =>
        LevelOf(tool.Id) < Tool.MaxLevel && NextCost(tool).All(c => Count(c.Key) >= c.Value);

    /// <summary>Створює інструмент (рівень 1) або покращує його на рівень.</summary>
    public bool Upgrade(Tool tool)
    {
        if (!CanUpgrade(tool)) return false;
        foreach (var cost in NextCost(tool)) Items[cost.Key] -= cost.Value;

        var level = LevelOf(tool.Id) + 1;
        ToolLevels[tool.Id] = level;
        Message?.Invoke(level == 1 ? $"СТВОРЕНО: {tool.Name}" : $"{tool.Name}: РІВЕНЬ {level}");
        AutoSave();
        Changed?.Invoke();
        return true;
    }

    /// <summary>Вкласти очко навичок: 0 - сила, 1 - витривалість.</summary>
    public bool SpendSkillPoint(int skill)
    {
        if (SkillPoints <= 0) return false;
        if (skill == 0) Strength++; else Endurance++;
        SkillPoints--;
        AutoSave();
        Changed?.Invoke();
        return true;
    }

    // ================= Бій =================

    /// <summary>Шкода героя за удар: кулаки (3) + найкраща зброя + 2 за кожне очко сили.</summary>
    public int Damage
    {
        get
        {
            var best = Catalog.Tools.Where(t => t.IsWeapon && HasTool(t.Id))
                .Select(t => t.Damage + (LevelOf(t.Id) - 1) * (t.Damage >= 10 ? 4 : 3)).DefaultIfEmpty(0).Max();
            return 3 + best + Strength * 2;
        }
    }

    public Tool? BestWeapon => Catalog.Tools.Where(t => t.IsWeapon && HasTool(t.Id)).OrderByDescending(t => t.Damage + LevelOf(t.Id)).FirstOrDefault();

    public bool CanFight => !IsBusy && Energy >= FightEnergyCost && Health > 20;

    public bool StartFight(Place place)
    {
        if (!CanFight) return false;
        var enemy = Catalog.Enemies[place.Id];
        Energy -= FightEnergyCost;
        LastFight = null;
        Fight = new ActiveFight { Enemy = enemy, Place = place, EnemyHp = enemy.Hp, HeroTimer = 0.5, EnemyTimer = EnemyAttackEvery };
        Changed?.Invoke();
        return true;
    }

    /// <summary>Підсумок бою для екрана результату.</summary>
    public record FightResult(bool Won, string LootItemId, int LootAmount, int Xp, bool LeveledUp);
    public FightResult? LastFight { get; private set; }

    private void FightStep(double dt)
    {
        var f = Fight!;
        f.HeroTimer -= dt;
        f.EnemyTimer -= dt;

        // Удар героя: розкид ±20% і 15% шанс критичного (подвійна шкода)
        if (f.HeroTimer <= 0)
        {
            f.HeroTimer += HeroAttackEvery;
            var crit = Random.Shared.NextDouble() < 0.15;
            var dmg = (int)Math.Round(Damage * (0.8 + Random.Shared.NextDouble() * 0.4)) * (crit ? 2 : 1);
            f.EnemyHp = Math.Max(0, f.EnemyHp - dmg);
            FightHit?.Invoke(true, dmg, crit);

            if (f.EnemyHp <= 0) { WinFight(); return; }
        }

        // Удар ворога
        if (f.EnemyTimer <= 0)
        {
            f.EnemyTimer += EnemyAttackEvery;
            var dmg = (int)Math.Round(f.Enemy.Damage * (0.8 + Random.Shared.NextDouble() * 0.4));
            Health = Math.Max(0, Health - dmg);
            FightHit?.Invoke(false, dmg, false);
        }
    }

    private void WinFight()
    {
        var f = Fight!;
        Fight = null;
        var loot = Random.Shared.Next(f.Enemy.LootMin, f.Enemy.LootMax + 1);
        Add(f.Enemy.LootItemId, loot);

        var levelBefore = Level;
        Message?.Invoke($"ПЕРЕМОГА! +{loot} {Catalog.ItemById(f.Enemy.LootItemId).Name}");
        GainXp(f.Enemy.Xp);
        LastFight = new FightResult(true, f.Enemy.LootItemId, loot, f.Enemy.Xp, Level > levelBefore);
        AutoSave();
    }

    /// <summary>Втекти з бою: енергія вже витрачена, ворог лишається живим.</summary>
    public void Flee()
    {
        if (Fight is null) return;
        Fight = null;
        LastFight = null;
        Message?.Invoke("ТИ ВТІК З БОЮ");
        Changed?.Invoke();
    }

    // ================= Непритомність =================

    /// <summary>Здоров'я скінчилось: герой непритомніє, втрачає половину ресурсів і прокидається ослабленим.</summary>
    private void Faint()
    {
        if (Fight is not null) LastFight = new FightResult(false, "", 0, 0, false);
        foreach (var key in Items.Keys.ToList()) Items[key] /= 2;
        Health = Hunger = Thirst = Energy = 40;
        Task = null;
        Fight = null;
        SleepRemaining = 0;
        Message?.Invoke("ТИ ВПАВ БЕЗ СИЛ. ВТРАЧЕНО ПОЛОВИНУ РЕСУРСІВ");
    }

    public int Count(string itemId) => Items.TryGetValue(itemId, out var n) ? n : 0;
    private void Add(string itemId, int amount) => Items[itemId] = Count(itemId) + amount;

    // ================= Збереження =================

    public static string SavePath => Path.Combine(FileSystem.AppDataDirectory, "survival-save.json");
    public static bool HasSave => File.Exists(SavePath);

    public void Save()
    {
        var data = new SaveData
        {
            Health = Health, Hunger = Hunger, Thirst = Thirst, Energy = Energy, Level = Level, Xp = Xp, PlaySeconds = PlaySeconds,
            Items = new Dictionary<string, int>(Items), ToolLevels = new Dictionary<string, int>(ToolLevels),
            SkillPoints = SkillPoints, Strength = Strength, Endurance = Endurance,
        };
        File.WriteAllText(SavePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>Завантажує гру з файлу. Повертає false, якщо файлу нема або він пошкоджений.</summary>
    public bool Load()
    {
        try
        {
            if (!File.Exists(SavePath)) return false;
            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
            if (data is null) return false;

            Health = data.Health; Hunger = data.Hunger; Thirst = data.Thirst; Energy = data.Energy;
            Level = data.Level; Xp = data.Xp; PlaySeconds = data.PlaySeconds;
            SkillPoints = data.SkillPoints; Strength = data.Strength; Endurance = data.Endurance;
            Items = new Dictionary<string, int>(data.Items);
            ToolLevels = new Dictionary<string, int>(data.ToolLevels);
            foreach (var oldTool in data.Tools) ToolLevels.TryAdd(oldTool, 1);   // збереження зі старої версії
            Task = null;
            Fight = null;
            SleepRemaining = 0;
            Started = true;
            _sinceSave = 0;
            Changed?.Invoke();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
