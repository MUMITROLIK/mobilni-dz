using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MovieSearch.Models;
using MovieSearch.Services;

namespace MovieSearch;

public partial class MainWindow : Window
{
    private readonly TmdbService? _tmdb;

    // Стан: що зараз показуємо (пошук чи каталог) і на якій ми сторінці.
    private string _currentQuery = "";
    private string _currentCategory = "popular";
    private int _page = 1;
    private int _totalPages = 1;
    private bool _loading;

    public MainWindow()
    {
        InitializeComponent();

        CatalogBox.ItemsSource = new List<KeyValuePair<string, string>>
        {
            new("Популярні", "popular"),
            new("Найвищий рейтинг", "top_rated"),
            new("Зараз у кіно", "now_playing"),
            new("Скоро в кіно", "upcoming"),
        };

        var key = TmdbService.LoadApiKey();
        if (string.IsNullOrEmpty(key))
        {
            StatusText.Text = "Немає API-ключа TMDB. Дивіться README: створіть appsettings.local.json або змінну TMDB_API_KEY.";
            SearchButton.IsEnabled = false;
            CatalogBox.IsEnabled = false;
            return;
        }

        _tmdb = new TmdbService(key);
        CatalogBox.SelectedIndex = 0; // це викликає завантаження "Популярних"
    }

    // Вибір каталогу: очищаємо рядок пошуку й показуємо категорію.
    private async void CatalogBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CatalogBox.SelectedValue is not string category) return;
        _currentCategory = category;
        _currentQuery = "";
        SearchBox.Text = "";
        await LoadAsync(1);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _currentQuery = SearchBox.Text.Trim();
        await LoadAsync(1);
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(_page - 1);
    private async void NextButton_Click(object sender, RoutedEventArgs e) => await LoadAsync(_page + 1);

    /// <summary>Завантажує сторінку: якщо є текст пошуку, то /search, інакше каталог.</summary>
    private async Task LoadAsync(int page)
    {
        if (_tmdb is null || _loading) return;
        _loading = true;
        StatusText.Text = "Завантаження...";
        try
        {
            var result = _currentQuery.Length > 0
                ? await _tmdb.SearchAsync(_currentQuery, page)
                : await _tmdb.GetCatalogAsync(_currentCategory, page);

            _page = result.Page;
            // TMDB віддає не більше 500 сторінок
            _totalPages = Math.Min(result.TotalPages, 500);
            MoviesList.ItemsSource = result.Results;
            ShowDetails(null);

            PageText.Text = $"Сторінка {_page} з {Math.Max(_totalPages, 1)}";
            PrevButton.IsEnabled = _page > 1;
            NextButton.IsEnabled = _page < _totalPages;
            StatusText.Text = result.Results.Count == 0 ? "Нічого не знайдено." : "";
        }
        catch (HttpRequestException ex)
        {
            StatusText.Text = "Помилка мережі або невірний ключ: " + ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private void MoviesList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShowDetails(MoviesList.SelectedItem as Movie);

    private void ShowDetails(Movie? movie)
    {
        DetailsPanel.Visibility = movie is null ? Visibility.Collapsed : Visibility.Visible;
        HintText.Visibility = movie is null ? Visibility.Visible : Visibility.Collapsed;
        if (movie is null) return;

        DetailTitle.Text = movie.Title;
        DetailMeta.Text = $"{movie.Year}   {movie.RatingText}   (голосів: {movie.VoteCount})";
        DetailOverview.Text = movie.OverviewText;
        DetailPoster.Source = movie.PosterUrl is null ? null : new BitmapImage(new Uri(movie.PosterUrl));
    }
}
