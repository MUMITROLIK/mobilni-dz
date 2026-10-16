using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using MovieSearch.Services;

namespace MovieSearch;

public partial class MainWindow : Window
{
    private readonly TmdbService? _tmdb;

    // Стан: що зараз показуємо (пошук чи каталог) і на якій ми сторінці.
    private string _currentQuery = "";
    private string _currentCategory = "popular";
    private string _currentTitle = "Популярні";
    private int _page = 1;
    private int _totalPages = 1;
    private bool _loading;

    public MainWindow()
    {
        InitializeComponent();

        var key = TmdbService.LoadApiKey();
        if (string.IsNullOrEmpty(key))
        {
            StatusText.Text = "Немає API-ключа TMDB. Дивіться README: створіть appsettings.local.json або змінну TMDB_API_KEY.";
            SearchButton.IsEnabled = false;
            TabsPanel.IsEnabled = false;
            return;
        }

        _tmdb = new TmdbService(key);
        PopularTab.IsChecked = true; // спрацьовує Checked і завантажується перший каталог
    }

    // Вибір вкладки каталогу: скидаємо пошук і показуємо категорію.
    private async void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string category } tab) return;
        _currentCategory = category;
        _currentTitle = tab.Content?.ToString() ?? "";
        _currentQuery = "";
        SearchBox.Text = "";
        await LoadAsync(1);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        if (query.Length == 0)
        {
            PopularTab.IsChecked = true;
            return;
        }

        _currentQuery = query;
        // Пошук не належить жодній вкладці, тому знімаємо виділення з усіх.
        foreach (var tab in TabsPanel.Children.OfType<RadioButton>()) tab.IsChecked = false;
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
            if (result.Results.Count > 0) MoviesList.ScrollIntoView(result.Results[0]);

            HeadingText.Text = _currentQuery.Length > 0 ? $"Результати за запитом «{_currentQuery}»" : _currentTitle;
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
}
