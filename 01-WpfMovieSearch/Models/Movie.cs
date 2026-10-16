using System.Globalization;
using System.Text.Json.Serialization;

namespace MovieSearch.Models;

/// <summary>Один фільм з відповіді TMDB. Атрибути JsonPropertyName зв'язують поля JSON з властивостями C#.</summary>
public class Movie
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("overview")] public string Overview { get; set; } = "";
    [JsonPropertyName("release_date")] public string? ReleaseDate { get; set; }
    [JsonPropertyName("vote_average")] public double VoteAverage { get; set; }
    [JsonPropertyName("vote_count")] public int VoteCount { get; set; }
    [JsonPropertyName("poster_path")] public string? PosterPath { get; set; }

    // Обчислювані властивості, які використовує XAML через Binding.
    public string? PosterUrl => string.IsNullOrEmpty(PosterPath) ? null : "https://image.tmdb.org/t/p/w342" + PosterPath;
    public string Year => ReleaseDate is { Length: >= 4 } ? ReleaseDate[..4] : "—";
    public string RatingText => VoteCount == 0 ? "немає оцінок" : "★ " + VoteAverage.ToString("0.0", CultureInfo.InvariantCulture) + " / 10";
    public string OverviewText => string.IsNullOrWhiteSpace(Overview) ? "Опис відсутній." : Overview;
}

/// <summary>Сторінка результатів: список фільмів + кількість сторінок.</summary>
public class MovieListResponse
{
    [JsonPropertyName("page")] public int Page { get; set; }
    [JsonPropertyName("results")] public List<Movie> Results { get; set; } = new();
    [JsonPropertyName("total_pages")] public int TotalPages { get; set; }
}
