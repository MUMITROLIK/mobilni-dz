using System.ComponentModel;

namespace Clicker.Models;

/// <summary>
/// Один товар магазину. Дані (назва, ціна, бонус) задаються в списку GameState.CreateShop(),
/// тому щоб додати новий товар, не треба писати нові кнопки та обробники.
/// INotifyPropertyChanged потрібен, щоб текст на кнопці сам оновлювався після покупки.
/// </summary>
public class ShopItem : INotifyPropertyChanged
{
    public string Id { get; init; } = "";          // ключ для збереження
    public string Name { get; init; } = "";
    public string Icon { get; init; } = "";        // файл з Resources/Images
    public long BaseCost { get; init; }
    public double CostMultiplier { get; init; } = 2;
    public long ClickBonus { get; init; }          // скільки монет за клік додає одна покупка
    public long PerSecondBonus { get; init; }      // скільки монет за секунду додає одна покупка

    private int _count;
    public int Count
    {
        get => _count;
        set { _count = value; Refresh(); }
    }

    /// <summary>Ціна росте з кожною покупкою: BaseCost * Multiplier^Count.</summary>
    public long Cost => (long)Math.Ceiling(BaseCost * Math.Pow(CostMultiplier, Count));

    public string ButtonText
    {
        get
        {
            var effect = ClickBonus > 0 ? $"+{ClickBonus} за клік" : $"+{PerSecondBonus}/с";
            return $"{Name}  ({effect})\nЦіна: {Cost}   Куплено: {Count}";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Повідомляє інтерфейс, що Cost і ButtonText змінилися.</summary>
    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Count)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Cost)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ButtonText)));
    }
}
