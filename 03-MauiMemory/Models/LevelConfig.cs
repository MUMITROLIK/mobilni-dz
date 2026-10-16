namespace MemoryGame.Models;

/// <summary>
/// Опис рівня складності. Усі правила гри залежать лише від цих чисел,
/// тому новий рівень додається одним рядком у <see cref="All"/>.
/// </summary>
public record LevelConfig(int Number, string Name, string Description, int Columns, int Rows, int Kinds, int CopiesPerKind)
{
    public int CardCount => Columns * Rows;

    /// <summary>Скільки карток треба відкрити парами, щоб виграти.</summary>
    public int PairCount => CardCount / 2;

    public string BestMovesKey => $"best_moves_{Number}";
    public string BestSecondsKey => $"best_seconds_{Number}";

    public static readonly LevelConfig[] All =
    {
        // 4x4, але лише 4 різні малюнки по 4 однакових: збігається будь-яка пара з чотирьох
        new(1, "ЛЕГКИЙ",   "4x4, 4 малюнки по 4 однакові", 4, 4, 4, 4),
        // класична гра: 4x4 і 8 різних пар
        new(2, "СЕРЕДНІЙ", "4x4, 8 різних пар",            4, 4, 8, 2),
        // велике поле: 6x6, кожна пара унікальна (18 різних малюнків)
        new(3, "СКЛАДНИЙ", "6x6, 18 унікальних пар",        6, 6, 18, 2),
    };

    public static LevelConfig Get(int number) => All.First(l => l.Number == number);
}
