using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace VoiceNotes.Services;

/// <summary>
/// Відправка запису в ШІ через OpenRouter. Деякі безкоштовні моделі приймають звук напряму,
/// тому окреме перетворення в текст не потрібне: модель сама слухає запис, розпізнає мову і робить конспект.
/// Модуль перенесено з попереднього завдання (чат з ШІ) і доповнено аудіо.
/// </summary>
public class AiService
{
    public const string DefaultModel = "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
    private const string Endpoint = "https://openrouter.ai/api/v1";
    private const string KeyPreference = "openrouter_api_key";
    private const string SecretsAsset = "secrets.local.json";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public string Model { get; set; } = DefaultModel;
    public string? ApiKey { get; private set; }
    public bool HasKey => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>Ключ беремо з secrets.local.json (вшивається під час збирання, у git не потрапляє) або з налаштувань пристрою.</summary>
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

    /// <summary>
    /// Надсилає файл у модель: звук кодується в base64 і йде в повідомленні як частина типу input_audio.
    /// Повертає розпізнаний текст і короткий конспект.
    /// </summary>
    public async Task<string> AnalyzeAsync(string audioPath, string format)
    {
        if (!HasKey) throw new InvalidOperationException("Немає ключа API.");

        var audio = Convert.ToBase64String(await File.ReadAllBytesAsync(audioPath));
        var request = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text =
                            "Це голосова нотатка. Зроби дві речі.\n" +
                            "1) ТЕКСТ: дослівно запиши, що сказано (мовою оригіналу).\n" +
                            "2) КОНСПЕКТ: стисло перекажи головне українською у 1-3 пунктах.\n" +
                            "Якщо в записі тиша або нерозбірливо, так і скажи." },
                        new { type = "input_audio", input_audio = new { data = audio, format } },
                    },
                },
            },
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        using var response = await Http.SendAsync(message);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(DescribeStatus((int)response.StatusCode, body));

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(text) ? "(порожня відповідь)" : text.Trim();
    }


    /// <summary>Модель для звичайного тексту: сама обирає вільну безкоштовну модель (як у чаті з попереднього завдання).</summary>
    public const string TextModel = "openrouter/free";

    /// <summary>
    /// Робить із тексту запису тези: короткий список головних думок. Це запасний шлях без звуку:
    /// працює навіть тоді, коли безкоштовна модель, що слухає звук, недоступна.
    /// </summary>
    public async Task<string> SummarizeTextAsync(string text)
    {
        if (!HasKey) throw new InvalidOperationException("Немає ключа API.");

        var request = new
        {
            model = TextModel,
            messages = new[]
            {
                new { role = "system", content = "Ти допомагаєш конспектувати. Відповідай українською." },
                new { role = "user", content =
                    "Ось текст голосової нотатки. Зроби з нього тези: 3-6 коротких пунктів, кожен з нового рядка і з тире на початку. " +
                    "Без вступу і висновків, лише головні думки.\n\n" + text },
            },
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{Endpoint}/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        using var response = await Http.SendAsync(message);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(DescribeStatus((int)response.StatusCode, body));

        using var doc = JsonDocument.Parse(body);
        var answer = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(answer) ? "(порожня відповідь)" : answer.Trim();
    }

    private static string DescribeStatus(int status, string body) => status switch
    {
        401 => "Ключ не підійшов. Перевір ключ OpenRouter.",
        402 => "Для роботи зі звуком OpenRouter вимагає баланс від $0.50. Поповни рахунок або обери іншу модель.",
        403 => "Ця модель недоступна у звичайних застосунках. Обери іншу модель.",
        429 => "Безкоштовна модель зараз перевантажена. Спробуй ще раз або обери іншу.",
        _ => $"Помилка {status}: " + (body.Length > 200 ? body[..200] : body),
    };

    /// <summary>Безкоштовні моделі, які вміють слухати звук (за даними OpenRouter).</summary>
    public async Task<List<string>> GetAudioModelsAsync()
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
                var hearsAudio = model.GetProperty("architecture").GetProperty("input_modalities").EnumerateArray()
                    .Any(m => m.GetString() == "audio");
                if (free && hearsAudio && id is not null && !result.Contains(id)) result.Add(id);
            }
        }
        catch (Exception)
        {
            // залишаємо тільки модель за замовчуванням
        }
        return result;
    }
}
