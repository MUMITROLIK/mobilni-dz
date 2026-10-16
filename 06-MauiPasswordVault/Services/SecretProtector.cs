using System.Security.Cryptography;
using System.Text;

namespace PassVault.Services;

/// <summary>
/// Шифрує паролі перед записом у файл алгоритмом AES-256-GCM. Ключ шифрування випадковий,
/// створюється один раз і лежить у захищеному сховищі пристрою (SecureStorage),
/// тобто окремо від файлу з паролями.
/// </summary>
public class SecretProtector
{
    private const string KeyName = "vault_aes_key";
    private byte[]? _key;

    private async Task<byte[]> GetKeyAsync()
    {
        if (_key is not null) return _key;

        string? stored = null;
        try { stored = await SecureStorage.Default.GetAsync(KeyName); }
        catch (Exception) { /* SecureStorage недоступний - нижче використаємо запасний файл */ }

        // Запасний варіант для середовищ без SecureStorage: файл ключа в приватній папці застосунку
        var fallbackPath = Path.Combine(FileSystem.AppDataDirectory, "vault.key");
        if (stored is null && File.Exists(fallbackPath)) stored = await File.ReadAllTextAsync(fallbackPath);

        if (stored is null)
        {
            stored = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            try { await SecureStorage.Default.SetAsync(KeyName, stored); }
            catch (Exception) { await File.WriteAllTextAsync(fallbackPath, stored); }
        }

        return _key = Convert.FromBase64String(stored);
    }

    /// <summary>Формат результату: nonce (12 байт) + тег (16 байт) + шифротекст, усе разом у base64.</summary>
    public async Task<string> ProtectAsync(string plain)
    {
        var key = await GetKeyAsync();
        var data = Encoding.UTF8.GetBytes(plain);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[data.Length];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, data, cipher, tag);

        return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
    }

    public async Task<string> UnprotectAsync(string secret)
    {
        if (string.IsNullOrEmpty(secret)) return "";
        try
        {
            var key = await GetKeyAsync();
            var all = Convert.FromBase64String(secret);
            var nonce = all[..12];
            var tag = all[12..28];
            var cipher = all[28..];
            var data = new byte[cipher.Length];

            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipher, tag, data);
            return Encoding.UTF8.GetString(data);
        }
        catch (Exception)
        {
            return "";   // ключ змінився або файл пошкоджено
        }
    }
}
