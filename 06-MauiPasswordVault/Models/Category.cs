namespace PassVault.Models;

/// <summary>Категорія пароля (соцмережі, ігри, робота...). Іконка - піксельна картинка з Resources/Images.</summary>
public record Category(string Id, string Name, string Icon)
{
    public static readonly Category All = new("all", "ВСІ", "cat_all.png");

    /// <summary>Категорії, які можна обрати для запису.</summary>
    public static readonly Category[] Items =
    {
        new("social", "СОЦМЕРЕЖІ", "cat_social.png"),
        new("games", "ІГРИ", "cat_games.png"),
        new("work", "РОБОТА", "cat_work.png"),
        new("mail", "ПОШТА", "cat_mail.png"),
        new("bank", "БАНК", "cat_bank.png"),
        new("other", "ІНШЕ", "cat_other.png"),
    };

    public static Category ById(string id) => Items.FirstOrDefault(c => c.Id == id) ?? Items[^1];
}
