using System.Text.Json;
using PassVault.Models;

namespace PassVault.Services;

/// <summary>Сховище записів: читає і пише JSON-файл у приватній папці застосунку, шифруючи паролі.</summary>
public class VaultStore
{
    public static VaultStore Instance { get; } = new();

    private static string FilePath => Path.Combine(FileSystem.AppDataDirectory, "vault.json");
    private readonly SecretProtector _protector = new();
    private List<VaultEntry> _entries = new();
    private bool _loaded;

    public async Task<List<EntryItem>> LoadAsync()
    {
        if (!_loaded)
        {
            try
            {
                if (File.Exists(FilePath))
                    _entries = JsonSerializer.Deserialize<List<VaultEntry>>(await File.ReadAllTextAsync(FilePath)) ?? new();
            }
            catch (JsonException) { _entries = new(); }
            _loaded = true;
        }

        var items = new List<EntryItem>();
        foreach (var e in _entries.OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase))
            items.Add(new EntryItem(e, await _protector.UnprotectAsync(e.Secret)));
        return items;
    }

    public async Task<EntryItem?> GetAsync(Guid id)
    {
        await LoadAsync();
        var entry = _entries.FirstOrDefault(e => e.Id == id);
        return entry is null ? null : new EntryItem(entry, await _protector.UnprotectAsync(entry.Secret));
    }

    /// <summary>Додає новий запис або оновлює існуючий (за Id).</summary>
    public async Task SaveAsync(VaultEntry entry, string plainPassword)
    {
        await LoadAsync();
        entry.Secret = await _protector.ProtectAsync(plainPassword);

        var index = _entries.FindIndex(e => e.Id == entry.Id);
        if (index >= 0) _entries[index] = entry; else _entries.Add(entry);
        await WriteAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        await LoadAsync();
        _entries.RemoveAll(e => e.Id == id);
        await WriteAsync();
    }

    private Task WriteAsync() =>
        File.WriteAllTextAsync(FilePath, JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true }));
}
