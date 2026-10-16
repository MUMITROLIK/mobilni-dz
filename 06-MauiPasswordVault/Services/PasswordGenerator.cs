using System.Security.Cryptography;

namespace PassVault.Services;

/// <summary>Генератор паролів і оцінка їхньої стійкості.</summary>
public static class PasswordGenerator
{
    // Схожі символи (0/O, 1/l/I) прибрано, щоб пароль було легко переписати вручну
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%^&*-_=+?";

    /// <summary>
    /// Створює пароль із обраних наборів символів. Використовується криптографічний генератор
    /// (RandomNumberGenerator), а не звичайний Random, бо Random передбачуваний.
    /// </summary>
    public static string Generate(int length, bool lower, bool upper, bool digits, bool symbols)
    {
        var sets = new List<string>();
        if (lower) sets.Add(Lower);
        if (upper) sets.Add(Upper);
        if (digits) sets.Add(Digits);
        if (symbols) sets.Add(Symbols);
        if (sets.Count == 0) sets.Add(Lower);          // хоча б щось має бути обрано

        length = Math.Max(length, sets.Count);
        var all = string.Concat(sets);
        var chars = new List<char>();

        foreach (var set in sets) chars.Add(set[RandomNumberGenerator.GetInt32(set.Length)]);   // по одному з кожного набору
        while (chars.Count < length) chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

        // Перемішуємо, щоб гарантовані символи не стояли завжди на початку
        for (var i = chars.Count - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }
        return new string(chars.ToArray());
    }

    /// <summary>Стійкість за ентропією: довжина помножена на log2 розміру алфавіту. Повертає рівень 0..4 і назву.</summary>
    public static (int Level, string Label) Evaluate(string password)
    {
        if (string.IsNullOrEmpty(password)) return (0, "ПОРОЖНІЙ");

        var pool = 0;
        if (password.Any(char.IsLower)) pool += 26;
        if (password.Any(char.IsUpper)) pool += 26;
        if (password.Any(char.IsDigit)) pool += 10;
        if (password.Any(c => !char.IsLetterOrDigit(c))) pool += 20;

        var bits = password.Length * Math.Log2(Math.Max(pool, 2));
        return bits switch
        {
            < 28 => (0, "ДУЖЕ СЛАБКИЙ"),
            < 40 => (1, "СЛАБКИЙ"),
            < 60 => (2, "СЕРЕДНІЙ"),
            < 90 => (3, "СИЛЬНИЙ"),
            _ => (4, "ДУЖЕ СИЛЬНИЙ"),
        };
    }
}
