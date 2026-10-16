using System.Net.Http;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
using MovieSearch.Models;

namespace MovieSearch.Services;

/// <summary>Клієнт для API TMDB (themoviedb.org). Усі запити тут, щоб вікно не знало деталей HTTP.</summary>
public class TmdbService
{
    private const string BaseUrl = "https://api.themoviedb.org/3";
    private readonly HttpClient _http = new();
    private readonly string _apiKey;

    // На сторінці TMDB є два варіанти: короткий "API Key" (v3) і довгий "Read Access Token" (v4, починається з eyJ).
    private bool IsBearerToken => _apiKey.StartsWith("eyJ");

    public TmdbService(string apiKey)
    {
        _apiKey = apiKey;
        if (IsBearerToken)
            _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
    }

    /// <summary>Ключ беремо зі змінної середовища TMDB_API_KEY або з appsettings.local.json (файл у .gitignore).</summary>
    public static string? LoadApiKey()
    {
        var fromEnv = Environment.GetEnvironmentVariable("TMDB_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv.Trim();

        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
        if (!File.Exists(path)) return null;

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.TryGetProperty("TmdbApiKey", out var v) ? v.GetString()?.Trim() : null;
    }

    /// <summary>Пошук за назвою: GET /search/movie?query=...</summary>
    public Task<MovieListResponse> SearchAsync(string query, int page) =>
        GetAsync("/search/movie", page, "query=" + Uri.EscapeDataString(query));

    /// <summary>Каталог: category = popular | top_rated | now_playing | upcoming.</summary>
    public Task<MovieListResponse> GetCatalogAsync(string category, int page) =>
        GetAsync("/movie/" + category, page);

    private async Task<MovieListResponse> GetAsync(string endpoint, int page, string? extra = null)
    {
        var auth = IsBearerToken ? "" : $"api_key={_apiKey}&";
        var url = $"{BaseUrl}{endpoint}?{auth}language=uk-UA&page={page}" + (extra is null ? "" : "&" + extra);
        var result = await _http.GetFromJsonAsync<MovieListResponse>(url);
        return result ?? new MovieListResponse();
    }
}
