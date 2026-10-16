using PassVault.Models;
using PassVault.Services;

namespace PassVault.Pages;

// Id запису приходить з параметра маршруту: EntryPage?id=...  (без параметра - це новий запис)
[QueryProperty(nameof(EntryId), "id")]
public partial class EntryPage : ContentPage
{
    private VaultEntry _entry = new();
    private Category _category = Category.Items[0];
    private readonly bool[] _sets = { true, true, true, false };   // a-z, A-Z, 0-9, символи
    private static readonly string[] SetNames = { "abc", "ABC", "123", "#$%" };

    public EntryPage()
    {
        InitializeComponent();
        BuildPresets();
        BuildSets();
        BuildCategories();
        UpdateStrength("");
    }

    // Shell кладе значення параметра сюди як рядок. Якщо запис існує, показуємо його для редагування.
    public string EntryId
    {
        set => _ = LoadAsync(value);
    }

    private async Task LoadAsync(string id)
    {
        if (!Guid.TryParse(id, out var guid)) return;
        var item = await VaultStore.Instance.GetAsync(guid);
        if (item is null) return;

        _entry = item.Entry;
        _category = Category.ById(_entry.CategoryId);
        PageTitle.Text = "РЕДАГУВАННЯ";
        NameEntry.Text = _entry.Name;
        SiteEntry.Text = _entry.Site;
        LoginEntry.Text = _entry.Login;
        TagEntry.Text = _entry.Tag;
        PasswordEntry.Text = item.PlainPassword;
        BuildCategories();
    }

    // ---------- Пароль: око і стійкість ----------

    private void OnEyeClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        EyeButton.ImageSource = PasswordEntry.IsPassword ? "icon_eye.png" : "icon_eye_off.png";
    }

    private void OnPasswordChanged(object? sender, TextChangedEventArgs e) => UpdateStrength(e.NewTextValue ?? "");

    private void UpdateStrength(string password)
    {
        var (level, label) = PasswordGenerator.Evaluate(password);
        var segments = new[] { Seg0, Seg1, Seg2, Seg3, Seg4 };
        string[] colors = { "#B13E53", "#EF7D57", "#FFCD75", "#A7F070", "#38B764" };

        for (var i = 0; i < segments.Length; i++)
            segments[i].Color = password.Length > 0 && i <= level ? Color.FromArgb(colors[level]) : Color.FromArgb("#333C57");
        StrengthLabel.Text = password.Length == 0 ? "" : label;
    }

    // ---------- Генератор ----------

    private void BuildPresets()
    {
        // Три готові рівні складності: довжина і набір символів
        var presets = new (string Name, int Length, bool[] Sets)[]
        {
            ("СЛАБКИЙ", 8, new[] { true, false, true, false }),
            ("СЕРЕДНІЙ", 12, new[] { true, true, true, false }),
            ("СИЛЬНИЙ", 20, new[] { true, true, true, true }),
        };

        foreach (var preset in presets)
        {
            var button = UiHelpers.MakeChip(preset.Name, "icon_dice.png", false, preset, (s, _) =>
            {
                if ((s as Button)?.BindingContext is not ValueTuple<string, int, bool[]> p) return;
                LengthSlider.Value = p.Item2;
                Array.Copy(p.Item3, _sets, _sets.Length);
                BuildSets();
                OnGenerateClicked(null, EventArgs.Empty);
            });
            button.ImageSource = null;
            PresetsRow.Add(button);
        }
    }

    private void BuildSets()
    {
        SetsRow.Clear();
        for (var i = 0; i < SetNames.Length; i++)
        {
            var index = i;
            var chip = UiHelpers.MakeChip(SetNames[i], null!, _sets[i], index, (_, _) =>
            {
                // хоча б один набір має лишатися увімкненим
                if (_sets[index] && _sets.Count(x => x) == 1) return;
                _sets[index] = !_sets[index];
                BuildSets();
            });
            chip.ImageSource = null;
            SetsRow.Add(chip);
        }
    }

    private void OnLengthChanged(object? sender, ValueChangedEventArgs e) =>
        LengthLabel.Text = $"ДОВЖИНА: {(int)Math.Round(e.NewValue)}";

    private void OnGenerateClicked(object? sender, EventArgs e)
    {
        PasswordEntry.Text = PasswordGenerator.Generate((int)Math.Round(LengthSlider.Value), _sets[0], _sets[1], _sets[2], _sets[3]);
        PasswordEntry.IsPassword = false;                 // одразу показуємо, що вийшло
        EyeButton.ImageSource = "icon_eye_off.png";
    }

    // ---------- Категорії ----------

    private void BuildCategories()
    {
        CategoryRow.Clear();
        foreach (var category in Category.Items)
            CategoryRow.Add(UiHelpers.MakeChip(category.Name, category.Icon, category == _category, category, (s, _) =>
            {
                if ((s as Button)?.BindingContext is not Category c) return;
                _category = c;
                BuildCategories();
            }));
    }

    // ---------- Збереження ----------

    private async void OnSaveClicked(object? sender, EventArgs e)
    {
        var name = NameEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (name.Length == 0 || password.Length == 0)
        {
            ErrorLabel.Text = "ВКАЖИ НАЗВУ І ПАРОЛЬ";
            ErrorLabel.IsVisible = true;
            return;
        }

        _entry.Name = name;
        _entry.Site = SiteEntry.Text?.Trim() ?? "";
        _entry.Login = LoginEntry.Text?.Trim() ?? "";
        _entry.Tag = TagEntry.Text?.Trim().TrimStart('#') ?? "";
        _entry.CategoryId = _category.Id;

        await VaultStore.Instance.SaveAsync(_entry, password);
        await Shell.Current.GoToAsync("..");
    }

    private async void OnCancelClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
