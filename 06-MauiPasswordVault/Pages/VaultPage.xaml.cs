using System.Collections.ObjectModel;
using PassVault.Models;
using PassVault.Services;

namespace PassVault.Pages;

public partial class VaultPage : ContentPage
{
    private readonly ObservableCollection<EntryItem> _view = new();   // те, що зараз видно (після пошуку і фільтра)
    private List<EntryItem> _all = new();
    private Category _selectedCategory = Category.All;
    private CancellationTokenSource? _clipboardTimer;

    public VaultPage()
    {
        InitializeComponent();
        EntriesList.ItemsSource = _view;
        BuildChips();
    }

    // Список перечитуємо щоразу, коли екран з'являється (після додавання чи редагування)
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _all = await VaultStore.Instance.LoadAsync();
        ApplyFilter();
    }

    // ---------- Пошук і категорії ----------

    private void BuildChips()
    {
        ChipsRow.Clear();
        foreach (var category in new[] { Category.All }.Concat(Category.Items))
            ChipsRow.Add(UiHelpers.MakeChip(category.Name, category.Icon, category == _selectedCategory, category, OnChipClicked));
    }

    private void OnChipClicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is not Category category) return;
        _selectedCategory = category;
        BuildChips();
        ApplyFilter();
    }

    private void OnSearchChanged(object? sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = SearchEntry.Text?.Trim() ?? "";
        var filtered = _all.Where(i =>
            (_selectedCategory == Category.All || i.Entry.CategoryId == _selectedCategory.Id) &&
            (query.Length == 0 ||
             i.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             i.Entry.Site.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             i.Entry.Login.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
             i.Entry.Tag.Contains(query, StringComparison.CurrentCultureIgnoreCase)));

        _view.Clear();
        foreach (var item in filtered) _view.Add(item);

        CountLabel.Text = $"{_all.Count} ЗАПИСІВ";
        EmptyLabel.Text = _all.Count == 0 ? "Тут поки порожньо. Додай перший пароль!" : "Нічого не знайдено.";
    }

    // ---------- Дії над записом ----------

    private static EntryItem? ItemOf(object? sender) => (sender as Button)?.BindingContext as EntryItem;

    /// <summary>Око: показати чи сховати пароль (за замовчуванням він закритий зірочками).</summary>
    private void OnRevealClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is { } item) item.IsRevealed = !item.IsRevealed;
    }

    /// <summary>Копіює пароль у буфер обміну і через 30 секунд очищає його, щоб пароль не лежав там вічно.</summary>
    private async void OnCopyClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item) return;

        await Clipboard.Default.SetTextAsync(item.PlainPassword);
        await ShowToastAsync("ПАРОЛЬ СКОПІЙОВАНО (ОЧИСТИТЬСЯ ЧЕРЕЗ 30 С)");

        _clipboardTimer?.Cancel();
        _clipboardTimer = new CancellationTokenSource();
        var token = _clipboardTimer.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    if (await Clipboard.Default.GetTextAsync() == item.PlainPassword)
                        await Clipboard.Default.SetTextAsync("");
                });
            }
            catch (OperationCanceledException) { }
        });
    }

    private async void OnEditClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is { } item)
            await Shell.Current.GoToAsync($"{nameof(EntryPage)}?id={item.Entry.Id}");
    }

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(EntryPage));

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        if (ItemOf(sender) is not { } item) return;

        var sure = await Dialog.ShowAsync("ВИДАЛИТИ?", $"Запис «{item.Name}» буде видалено назавжди.", "ВИДАЛИТИ", "СКАСУВАТИ");
        if (!sure) return;

        await VaultStore.Instance.DeleteAsync(item.Entry.Id);
        _all.Remove(item);
        ApplyFilter();
    }

    private async Task ShowToastAsync(string text)
    {
        ToastLabel.Text = text;
        ToastLabel.IsVisible = true;
        await Task.Delay(2200);
        ToastLabel.IsVisible = false;
    }
}
