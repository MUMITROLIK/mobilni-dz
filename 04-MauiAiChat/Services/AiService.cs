using System.ClientModel;
using System.Net.Http.Json;
using System.Text.Json;
using AiChat.Models;
using OpenAI;
using OpenAI.Chat;

namespace AiChat.Services;

/// <summary>
/// Уся робота з ШІ в одному місці. OpenRouter сумісний з API OpenAI, тому беремо офіційну
/// бібліотеку OpenAI з NuGet і просто вказуємо їй іншу адресу сервера.
/// </summary>
public class AiService
{
    public const string DefaultModel = "openrouter/free";   // сам вибирає вільну безкоштовну модель
    private const string Endpoint = "https://openrouter.ai/api/v1";
    private const string KeyPreference = "openrouter_api_key";
    private const string SecretsAsset = "secrets.local.json";

    private static readonly HttpClient Http = new();

    public string Model { get; set; } = DefaultModel;
    public string? ApiKey { get; private set; }
    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Шукаємо ключ: спочатку у файлі secrets.local.json, який вшивається в застосунок під час збирання
    /// (файл у .gitignore), якщо його нема - у збережених налаштуваннях користувача.
    /// </summary>
    public async Task LoadKeyAsync()
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(SecretsAsset);
            using var doc = await JsonDocument.ParseAsync(stream);
            if (doc.RootElement.TryGetProperty("OpenRouterApiKey", out var value) && !string.IsNullOrWhiteSpace(value.GetString()))
            {
                ApiKey = value.GetString()!.Trim();
                return;
            }
        }
        catch (Exception)
        {
            // файлу нема - це нормально для чистого клона репозиторію
        }

        var saved = Preferences.Get(KeyPreference, "");
        ApiKey = string.IsNullOrWhiteSpace(saved) ? null : saved;
    }

    public void SaveKey(string key)
    {
        ApiKey = key.Trim();
        Preferences.Set(KeyPreference, ApiKey);
    }

    /// <summary>Надсилає всю історію розмови, щоб ШІ пам'ятав контекст, і повертає текст відповіді.</summary>
    public async Task<string> AskAsync(IEnumerable<ChatLine> history)
    {
        if (!HasKey) throw new InvalidOperationException("Немає ключа API.");

        var client = new ChatClient(
            Model,
            new ApiKeyCredential(ApiKey!),
            new OpenAIClientOptions { Endpoint = new Uri(Endpoint) });

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Ти дружній помічник у піксельній грі-чаті. Відповідай мовою користувача, коротко і зрозуміло."),
        };
        foreach (var line in history.Where(l => !l.IsError && !l.IsLocal))
            messages.Add(line.IsUser ? new UserChatMessage(line.Text) : new AssistantChatMessage(line.Text));

        ChatCompletion completion = await client.CompleteChatAsync(messages);
        return completion.Content.Count > 0 ? completion.Content[0].Text : "(порожня відповідь)";
    }

    /// <summary>
    /// Список безкоштовних моделей з OpenRouter (ціна 0). Якщо мережі нема, повертаємо короткий запасний список.
    /// </summary>
    public async Task<List<string>> GetFreeModelsAsync()
    {
        var result = new List<string> { DefaultModel };
        try
        {
            var json = await Http.GetFromJsonAsync<JsonElement>($"{Endpoint}/models");
            foreach (var model in json.GetProperty("data").EnumerateArray())
            {
                var id = model.GetProperty("id").GetString();
                var pricing = model.GetProperty("pricing");
                var free = pricing.GetProperty("prompt").GetString() == "0" && pricing.GetProperty("completion").GetString() == "0";
                if (free && id is not null && !result.Contains(id)) result.Add(id);
            }
        }
        catch (Exception)
        {
            // залишаємо тільки модель за замовчуванням
        }
        return result;
    }
}
