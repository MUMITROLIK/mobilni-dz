namespace Survival.Models;

/// <summary>Усе, що записується у файл збереження (JSON).</summary>
public class SaveData
{
    public double Health { get; set; } = 100;
    public double Hunger { get; set; } = 100;
    public double Energy { get; set; } = 100;
    public int Level { get; set; } = 1;
    public int Xp { get; set; }
    public double PlaySeconds { get; set; }
    public Dictionary<string, int> Items { get; set; } = new();
    public List<string> Tools { get; set; } = new();
}
