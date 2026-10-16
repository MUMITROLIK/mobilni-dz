namespace Survival.Models;

/// <summary>Предмет (ресурс). Якщо FoodValue більше нуля, його можна з'їсти, і він поповнить ситість.</summary>
public record Item(string Id, string Name, string Icon, int FoodValue = 0);

/// <summary>Одна дія збору ресурсу в локації: скільки триває, скільки енергії коштує і що дає.</summary>
public record GatherAction(string Name, string ItemId, int MinYield, int MaxYield, int EnergyCost, int Xp, int Seconds = 10);

/// <summary>Локація. Доступна, коли рівень гравця не менший за RequiredLevel.</summary>
public record Place(string Id, string Name, string Icon, int RequiredLevel, string Description, GatherAction[] Actions);

/// <summary>Інструмент, який гравець створює з ресурсів. Дає +1 до видобутку вказаних ресурсів.</summary>
public record Tool(string Id, string Name, string Icon, string Description, string[] BoostedItems, Dictionary<string, int> Cost);

/// <summary>
/// Усі дані гри в одному місці: щоб додати новий ресурс чи локацію, достатньо дописати рядок сюди,
/// а екрани побудуються самі.
/// </summary>
public static class Catalog
{
    public static readonly Item[] Items =
    {
        new("wood", "ДЕРЕВО", "res_wood.png"),
        new("stone", "КАМІНЬ", "res_stone.png"),
        new("berry", "ЯГОДИ", "res_berry.png", FoodValue: 10),
        new("mushroom", "ГРИБИ", "res_mushroom.png", FoodValue: 14),
        new("cactus", "КАКТУС", "res_cactus.png", FoodValue: 8),
        new("fish", "РИБА", "res_fish.png", FoodValue: 25),
        new("ice", "ЛІД", "res_ice.png"),
        new("ore", "РУДА", "res_ore.png"),
        new("crystal", "КРИСТАЛ", "res_crystal.png"),
    };

    public static readonly Place[] Locations =
    {
        new("forest", "ЛІС", "loc_forest.png", 1, "Дерево, ягоди і гриби. Гарне місце для початку.", new[]
        {
            new GatherAction("РУБАТИ ДЕРЕВО", "wood", 2, 4, 12, 6),
            new GatherAction("ЗБИРАТИ ЯГОДИ", "berry", 2, 5, 6, 4),
            new GatherAction("ШУКАТИ ГРИБИ", "mushroom", 1, 3, 8, 5),
        }),
        new("desert", "ПУСТЕЛЯ", "loc_desert.png", 2, "Спекотно. Багато каміння та кактусів.", new[]
        {
            new GatherAction("ДОБУВАТИ КАМІНЬ", "stone", 2, 4, 14, 8),
            new GatherAction("ЗРІЗАТИ КАКТУС", "cactus", 2, 4, 8, 5),
        }),
        new("tundra", "ТУНДРА", "loc_tundra.png", 3, "Холодно, але в ополонках є риба.", new[]
        {
            new GatherAction("КОЛОТИ ЛІД", "ice", 2, 3, 12, 8),
            new GatherAction("ЛОВИТИ РИБУ", "fish", 1, 3, 10, 9),
        }),
        new("cave", "ПЕЧЕРА", "loc_cave.png", 4, "Темно й небезпечно, зате є цінна руда.", new[]
        {
            new GatherAction("ДОБУВАТИ РУДУ", "ore", 1, 3, 16, 12),
            new GatherAction("ШУКАТИ КРИСТАЛИ", "crystal", 1, 2, 18, 15),
            new GatherAction("ДОБУВАТИ КАМІНЬ", "stone", 3, 5, 14, 8),
        }),
    };

    public static readonly Tool[] Tools =
    {
        new("axe", "СОКИРА", "tool_axe.png", "+1 дерево за збір", new[] { "wood" }, new() { ["wood"] = 4, ["stone"] = 2 }),
        new("pickaxe", "КИРКА", "tool_pickaxe.png", "+1 камінь, руда і кристал", new[] { "stone", "ore", "crystal" }, new() { ["wood"] = 3, ["stone"] = 4 }),
        new("rod", "УДОЧКА", "tool_rod.png", "+1 риба за збір", new[] { "fish" }, new() { ["wood"] = 5, ["ice"] = 2 }),
    };

    public static Item ItemById(string id) => Items.First(i => i.Id == id);
    public static Place LocationById(string id) => Locations.First(l => l.Id == id);
    public static Tool ToolById(string id) => Tools.First(t => t.Id == id);
}
