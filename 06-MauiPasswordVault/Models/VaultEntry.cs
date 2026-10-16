namespace PassVault.Models;

/// <summary>
/// Один запис у сховищі. У файл потрапляє лише зашифрований пароль (поле Secret),
/// відкритого пароля на диску ніколи немає.
/// </summary>
public class VaultEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Site { get; set; } = "";
    public string Login { get; set; } = "";
    public string Secret { get; set; } = "";       // зашифрований пароль (base64)
    public string CategoryId { get; set; } = "other";
    public string Tag { get; set; } = "";          // вільний тег, наприклад назва компанії
    public DateTime Created { get; set; } = DateTime.Now;
}
