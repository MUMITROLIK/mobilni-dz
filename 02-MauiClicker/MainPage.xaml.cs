using System.Text.Json;
using Clicker.Models;

namespace Clicker;

public partial class MainPage : ContentPage
{
    // Файл збереження лежить у папці даних застосунку (працює на Windows і Android).
    private static string SavePath => Path.Combine(FileSystem.AppDataDirectory, "clicker-save.json");

    private long _coins;
    private long _totalClicks;
    private readonly List<ShopItem> _shop = CreateShop();

    public MainPage()
    {
        InitializeComponent();

        // Вікно має відкриватися зверху, а не посередині списку
        Loaded += async (_, _) => await MainScroll.ScrollToAsync(0, 0, false);

        BindableLayout.SetItemsSource(ShopLayout, _shop);

        // Автоклікер: таймер, що спрацьовує раз на секунду (замість нескінченного while з Task.Delay).
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromSeconds(1);
        timer.Tick += (_, _) =>
        {
            _coins += PerSecond;
            UpdateUI();
        };
        timer.Start();

        // Якщо збереження вже є, підхоплюємо його на старті.
        if (File.Exists(SavePath)) LoadGame(showMessage: false);
        UpdateUI();
    }

    /// <summary>Каталог товарів. Додати новий товар = додати один рядок тут.</summary>
    private static List<ShopItem> CreateShop() => new()
    {
        new() { Id = "power",   Name = "Покращити клік", Icon = "icon_power.png",   BaseCost = 10,    CostMultiplier = 1.6, ClickBonus = 1 },
        new() { Id = "finger",  Name = "Золотий палець", Icon = "icon_finger.png",  BaseCost = 120,   CostMultiplier = 1.8, ClickBonus = 8 },
        new() { Id = "auto",    Name = "Автоклікер",     Icon = "icon_auto.png",    BaseCost = 50,    CostMultiplier = 1.5, PerSecondBonus = 1 },
        new() { Id = "farm",    Name = "Ферма",          Icon = "icon_farm.png",    BaseCost = 400,   CostMultiplier = 1.6, PerSecondBonus = 6 },
        new() { Id = "factory", Name = "Фабрика",        Icon = "icon_factory.png", BaseCost = 2500,  CostMultiplier = 1.7, PerSecondBonus = 35 },
        new() { Id = "mine",    Name = "Шахта",          Icon = "icon_mine.png",    BaseCost = 15000, CostMultiplier = 1.8, PerSecondBonus = 200 },
    };

    // Сила кліка й дохід за секунду рахуємо з куплених товарів, тому їх не треба окремо зберігати.
    private long ClickPower => 1 + _shop.Sum(i => i.ClickBonus * i.Count);
    private long PerSecond => _shop.Sum(i => i.PerSecondBonus * i.Count);

    private async void OnClickButtonClicked(object? sender, EventArgs e)
    {
        _coins += ClickPower;
        _totalClicks++;
        UpdateUI();

        // Маленька анімація "натискання" монети
        await CoinButton.ScaleTo(0.9, 50);
        await CoinButton.ScaleTo(1.0, 50);
    }

    // Один обробник на всі товари: BindingContext кнопки - це її ShopItem.
    private void OnBuyClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not ShopItem item) return;

        if (_coins < item.Cost)
        {
            StatusLabel.Text = $"Не вистачає монет на «{item.Name}».";
            return;
        }

        _coins -= item.Cost;
        item.Count++;
        StatusLabel.Text = "";
        UpdateUI();
    }

    private void OnSaveClicked(object? sender, EventArgs e)
    {
        var data = new SaveData { Coins = _coins, TotalClicks = _totalClicks };
        foreach (var item in _shop) data.Purchases[item.Id] = item.Count;

        File.WriteAllText(SavePath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
        StatusLabel.Text = "Гру збережено.";
    }

    private void OnLoadClicked(object? sender, EventArgs e) => LoadGame(showMessage: true);

    private void LoadGame(bool showMessage)
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                if (showMessage) StatusLabel.Text = "Збереження ще не створене.";
                return;
            }

            var data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath));
            if (data is null) return;

            _coins = data.Coins;
            _totalClicks = data.TotalClicks;
            foreach (var item in _shop)
                item.Count = data.Purchases.TryGetValue(item.Id, out var count) ? count : 0;

            UpdateUI();
            if (showMessage) StatusLabel.Text = "Гру завантажено.";
        }
        catch (JsonException)
        {
            StatusLabel.Text = "Файл збереження пошкоджено.";
        }
    }

    private async void OnResetClicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlert("Скинути гру?", "Весь прогрес буде втрачено (файл збереження не видаляється, доки ви не збережете знову).", "Скинути", "Скасувати"))
            return;

        _coins = 0;
        _totalClicks = 0;
        foreach (var item in _shop) item.Count = 0;
        UpdateUI();
        StatusLabel.Text = "Гру скинуто.";
    }

    private void UpdateUI()
    {
        CoinsLabel.Text = $"Монети: {_coins}";
        ClickPowerLabel.Text = $"За клік: {ClickPower}   (кліків: {_totalClicks})";
        PerSecondLabel.Text = $"За секунду: {PerSecond}";
    }
}
