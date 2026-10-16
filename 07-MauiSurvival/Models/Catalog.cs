namespace Survival.Models;

/// <summary>Предмет (ресурс). FoodValue поповнює ситість, DrinkValue втамовує спрагу.</summary>
public record Item(string Id, string Name, string Icon, int FoodValue = 0, int DrinkValue = 0);

/// <summary>Одна дія збору ресурсу в локації: скільки триває, скільки енергії коштує і що дає.</summary>
public record GatherAction(string Name, string ItemId, int MinYield, int MaxYield, int EnergyCost, int Xp, int Seconds = 10);

/// <summary>Локація. Доступна, коли рівень гравця не менший за RequiredLevel. Тут же живе її ворог.</summary>
public record Place(string Id, string Name, string Icon, int RequiredLevel, string Description, GatherAction[] Actions);

/// <summary>Ворог локації: здоров'я, удар, нагорода (досвід і здобич).</summary>
public record Enemy(string Id, string Name, string Image, int Hp, int Damage, int Xp, string LootItemId, int LootMin, int LootMax);

/// <summary>
/// Інструмент або зброя. Інструмент дає +рівень до видобутку вказаних ресурсів, зброя додає шкоди в бою.
/// Кожен можна покращувати до MaxLevel за ресурси (вартість росте з рівнем).
/// </summary>
public record Tool(string Id, string Name, string Icon, string Description, string[] BoostedItems, Dictionary<string, int> Cost, int Damage = 0)
{
    public const int MaxLevel = 3;
    public bool IsWeapon => Damage > 0;

    /// <summary>Вартість переходу на наступний рівень: базова вартість, помножена на номер рівня.</summary>
    public Dictionary<string, int> CostOfLevel(int level) => Cost.ToDictionary(c => c.Key, c => c.Value * level);
}

/// <summary>
/// Усі дані гри в одному місці: щоб додати новий ресурс, локацію чи ворога, достатньо дописати рядок сюди,
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
        new("cactus", "КАКТУС", "res_cactus.png", FoodValue: 8, DrinkValue: 10),
        new("fish", "РИБА", "res_fish.png", FoodValue: 25),
        new("meat", "М'ЯСО", "res_meat.png", FoodValue: 32),
        new("water", "ВОДА", "res_water.png", DrinkValue: 35),
        new("ice", "ЛІД", "res_ice.png", DrinkValue: 18),
        new("ore", "РУДА", "res_ore.png"),
        new("crystal", "КРИСТАЛ", "res_crystal.png"),
    };

    public static readonly Place[] Locations =
    {
        new("forest", "ЛІС", "loc_forest.png", 1, "Дерево, ягоди, гриби й чиста вода з річки.", new[]
        {
            new GatherAction("РУБАТИ ДЕРЕВО", "wood", 2, 4, 12, 6),
            new GatherAction("ЗБИРАТИ ЯГОДИ", "berry", 2, 5, 6, 4),
            new GatherAction("ШУКАТИ ГРИБИ", "mushroom", 1, 3, 8, 5),
            new GatherAction("НАБИРАТИ ВОДУ", "water", 2, 4, 6, 4),
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

    /// <summary>Вороги за локаціями: чим далі, тим міцніші, але й нагорода краща.</summary>
    public static readonly Dictionary<string, Enemy> Enemies = new()
    {
        ["forest"] = new("slime", "СЛИЗ", "k_slime.png", 24, 4, 10, "meat", 1, 2),
        ["desert"] = new("spider", "ПАВУК", "k_spider.png", 34, 6, 14, "meat", 1, 3),
        ["tundra"] = new("ghost", "ПРИВИД", "k_ghost.png", 44, 7, 18, "meat", 2, 3),
        ["cave"] = new("bat", "КАЖАН", "k_bat.png", 60, 9, 24, "crystal", 1, 2),
    };

    public static readonly Tool[] Tools =
    {
        new("axe", "СОКИРА", "tool_axe.png", "+рівень до дерева за збір", new[] { "wood" }, new() { ["wood"] = 4, ["stone"] = 2 }),
        new("pickaxe", "КИРКА", "tool_pickaxe.png", "+рівень до каменю, руди, кристалів", new[] { "stone", "ore", "crystal" }, new() { ["wood"] = 3, ["stone"] = 4 }),
        new("rod", "УДОЧКА", "tool_rod.png", "+рівень до риби за збір", new[] { "fish" }, new() { ["wood"] = 5, ["ice"] = 2 }),
        new("spear", "СПИС", "tool_spear.png", "зброя: удар +6 (кожен рівень +3)", Array.Empty<string>(), new() { ["wood"] = 3, ["stone"] = 2 }, Damage: 6),
        new("sword", "МЕЧ", "tool_sword.png", "зброя: удар +10 (кожен рівень +4)", Array.Empty<string>(), new() { ["wood"] = 2, ["ore"] = 3 }, Damage: 10),
    };

    public static Item ItemById(string id) => Items.First(i => i.Id == id);
    public static Place LocationById(string id) => Locations.First(l => l.Id == id);
    public static Tool ToolById(string id) => Tools.First(t => t.Id == id);
}
