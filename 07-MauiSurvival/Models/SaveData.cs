namespace Survival.Models;

/// <summary>Усе, що записується у файл збереження (JSON). Нові поля мають значення за замовчуванням, тому старі збереження теж читаються.</summary>
public class SaveData
{
    public double Health { get; set; } = 100;
    public double Hunger { get; set; } = 100;
    public double Thirst { get; set; } = 100;
    public double Energy { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public double PlaySeconds { get; set; }
    public Dictionary<string, int> Items { get; set; } = new();

    /// <summary>Рівні інструментів і зброї (0 або відсутній - ще не створено).</summary>
    public Dictionary<string, int> ToolLevels { get; set; } = new();

    // Навички героя
    public int SkillPoints { get; set; }
    public int Strength { get; set; }
    public int Endurance { get; set; }

    // Старе поле (до появи рівнів інструментів): читаємо його для сумісності
    public List<string> Tools { get; set; } = new();
}
