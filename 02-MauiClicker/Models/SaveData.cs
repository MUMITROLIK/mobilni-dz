namespace Clicker.Models;

/// <summary>Те, що записується у файл збереження (JSON).</summary>
public class SaveData
{
    public long Coins { get; set; }
    public long TotalClicks { get; set; }

    /// <summary>Id товару -> скільки куплено. Так нові товари не ламають старі збереження.</summary>
    public Dictionary<string, int> Purchases { get; set; } = new();
}
