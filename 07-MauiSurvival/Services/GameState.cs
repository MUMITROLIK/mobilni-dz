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

/// <summary>
/// Стан гри: характеристики, інвентар, рівень і поточне завдання. Один спільний об'єкт на всі екрани,
/// тому, коли ми переходимо між екранами, гра не губить прогрес, а збір ресурсів іде далі.
/// Ігровий час рухає таймер: кожні 250 мс викликається Tick.
/// </summary>
public class GameState
{
    public static GameState Current { get; } = new();

    public const double SleepSeconds = 10;
    private const double SecondsPerDay = 120;

    // ----- Характеристики (0..100) -----
    public double Health { get; private set; } = 100;
    public double Hunger { get; private set; } = 100;    // ситість: 100 - ситий, 0 - голодний
    public double Energy { get; private set; } = 100;

    // ----- Прогрес -----
    public int Level { get; private set; } = 1;
    public int Xp { get; private set; }
    public int XpToNext => Level * 25;
    public double PlaySeconds { get; private set; }
    public int Day => 1 + (int)(PlaySeconds / SecondsPerDay);

    // ----- Інвентар і інструменти -----
    public Dictionary<string, int> Items { get; private set; } = new();
    public HashSet<string> Tools { get; private set; } = new();

    // ----- Поточна дія -----
    public ActiveTask? Task { get; private set; }
    public double SleepRemaining { get; private set; }
    public bool IsSleeping => SleepRemaining > 0;
    public bool IsBusy => Task is not null || IsSleeping;

    /// <summary>Чи розпочата гра (щоб кнопка «Продовжити» мала сенс).</summary>
    public bool Started { get; private set; }

    /// <summary>Щось змінилось: екрани перемальовують смуги і списки.</summary>
    public event Action? Changed;

    /// <summary>Повідомлення для гравця (наприклад, «+3 ДЕРЕВО»).</summary>
    public event Action<string>? Message;

    private IDispatcherTimer? _timer;

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

    /// <summary>Один крок ігрового часу: голод, відновлення енергії, виконання завдання.</summary>
    public void Tick(double dt)
    {
        if (!Started) return;
        PlaySeconds += dt;

        // Голод росте завжди, а під час сну повільніше
        Hunger = Math.Max(0, Hunger - dt / (IsSleeping ? 12.0 : 6.0));

        // Енергія відновлюється: швидко уві сні, повільно у спокої, під час роботи не відновлюється
        if (IsSleeping) Energy = Math.Min(100, Energy + dt * 6);
        else if (Task is null) Energy = Math.Min(100, Energy + dt * 0.35);

        // Без їжі здоров'я падає, а на ситий шлунок повільно відновлюється
        if (Hunger <= 0) Health = Math.Max(0, Health - dt * 0.8);
        else if (Hunger > 60 && Energy > 30) Health = Math.Min(100, Health + dt * 0.3);

        if (IsSleeping) SleepRemaining = Math.Max(0, SleepRemaining - dt);

        if (Task is not null)
        {
            Task.Remaining -= dt;
            if (Task.Remaining <= 0) FinishTask();
        }

        if (Health <= 0) Faint();
        Changed?.Invoke();
    }

    // ================= Дії =================

    public void NewGame()
    {
        Health = Hunger = Energy = 100;
        Level = 1;
        Xp = 0;
        PlaySeconds = 0;
        Items = new();
        Tools = new();
        Task = null;
        SleepRemaining = 0;
        Started = true;
        Changed?.Invoke();
    }

    public bool CanStart(GatherAction action) => !IsBusy && Energy >= action.EnergyCost;

    /// <summary>Запускає збір на 10 секунд: витрачає енергію одразу, а ресурси видає в кінці.</summary>
    public bool StartTask(Place location, GatherAction action)
    {
        if (!CanStart(action)) return false;
        Energy -= action.EnergyCost;
        Task = new ActiveTask { Action = action, Place = location, Remaining = action.Seconds };
        Changed?.Invoke();
        return true;
    }

    /// <summary>Підсумок останнього збору: показується на екрані сцени після завершення.</summary>
    public record Gain(string ItemId, int Amount, int Xp, bool LeveledUp);
    public Gain? LastGain { get; private set; }

    private void FinishTask()
    {
        var task = Task!;
        Task = null;

        var amount = Random.Shared.Next(task.Action.MinYield, task.Action.MaxYield + 1);
        if (Tools.Any(t => Catalog.ToolById(t).BoostedItems.Contains(task.Action.ItemId))) amount++;

        Add(task.Action.ItemId, amount);
        var levelBefore = Level;
        Message?.Invoke($"+{amount} {Catalog.ItemById(task.Action.ItemId).Name}");
        GainXp(task.Action.Xp);
        LastGain = new Gain(task.Action.ItemId, amount, task.Action.Xp, Level > levelBefore);
    }

    private void GainXp(int xp)
    {
        Xp += xp;
        while (Xp >= XpToNext)
        {
            Xp -= XpToNext;
            Level++;
            Health = 100;
            Message?.Invoke($"НОВИЙ РІВЕНЬ: {Level}!");
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

    public bool CanCraft(Tool tool) => !Tools.Contains(tool.Id) && tool.Cost.All(c => Count(c.Key) >= c.Value);

    public bool Craft(Tool tool)
    {
        if (!CanCraft(tool)) return false;
        foreach (var cost in tool.Cost) Items[cost.Key] -= cost.Value;
        Tools.Add(tool.Id);
        Message?.Invoke($"СТВОРЕНО: {tool.Name}");
        Changed?.Invoke();
        return true;
    }

    /// <summary>Здоров'я скінчилось: герой непритомніє, втрачає половину ресурсів і прокидається ослабленим.</summary>
    private void Faint()
    {
        foreach (var key in Items.Keys.ToList()) Items[key] /= 2;
        Health = Hunger = Energy = 40;
        Task = null;
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
            Health = Health, Hunger = Hunger, Energy = Energy, Level = Level, Xp = Xp, PlaySeconds = PlaySeconds,
            Items = new Dictionary<string, int>(Items), Tools = Tools.ToList(),
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

            Health = data.Health; Hunger = data.Hunger; Energy = data.Energy;
            Level = data.Level; Xp = data.Xp; PlaySeconds = data.PlaySeconds;
            Items = new Dictionary<string, int>(data.Items);
            Tools = data.Tools.ToHashSet();
            Task = null;
            SleepRemaining = 0;
            Started = true;
            Changed?.Invoke();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
