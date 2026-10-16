using System.ClientModel;
using System.Collections.ObjectModel;
using AiChat.Models;
using AiChat.Services;

namespace AiChat.Pages;

public partial class ChatPage : ContentPage
{
    private readonly AiService _ai = new();
    private readonly ObservableCollection<ChatLine> _lines = new();   // розмова; ObservableCollection сама оновлює список на екрані
    private bool _waiting;
    private bool _started;

    public ChatPage()
    {
        InitializeComponent();
        Messages.ItemsSource = _lines;
        UpdateModelButton();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started) return;
        _started = true;

        await _ai.LoadKeyAsync();
        if (!_ai.HasKey) KeyOverlay.IsVisible = true;
        AddGreeting();

        // Список безкоштовних моделей підтягуємо у фоні
        ModelList.ItemsSource = await _ai.GetFreeModelsAsync();
    }

    private void AddGreeting() =>
        _lines.Add(new ChatLine { IsLocal = true, Text = "Привіт! Я твій піксельний помічник. Напиши мені щось, і я відповім." });

    // ---------- Надсилання повідомлення ----------

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        var text = InputEntry.Text?.Trim();
        if (string.IsNullOrEmpty(text) || _waiting) return;

        InputEntry.Text = "";
        _lines.Add(new ChatLine { IsUser = true, Text = text });
        ScrollToEnd();
        SetWaiting(true);

        try
        {
            var answer = await _ai.AskAsync(_lines);
            _lines.Add(new ChatLine { Text = answer });
        }
        catch (Exception ex)
        {
            _lines.Add(new ChatLine { IsError = true, Text = DescribeError(ex) });
        }
        finally
        {
            SetWaiting(false);
            ScrollToEnd();
            InputEntry.Focus();
        }
    }

    /// <summary>Перетворює технічні помилки на зрозумілі повідомлення.</summary>
    private static string DescribeError(Exception ex) => ex switch
    {
        ClientResultException { Status: 401 } => "Ключ не підійшов. Перевір ключ OpenRouter.",
        ClientResultException { Status: 402 } => "На рахунку немає коштів для цієї моделі. Обери безкоштовну.",
        ClientResultException { Status: 429 } => "Безкоштовна модель зараз перевантажена. Спробуй ще раз або обери іншу модель.",
        HttpRequestException => "Немає зв'язку з сервером. Перевір інтернет.",
        _ => "Помилка: " + ex.Message,
    };

    private void ScrollToEnd()
    {
        if (_lines.Count == 0) return;
        Dispatcher.Dispatch(() => Messages.ScrollTo(_lines.Count - 1, position: ScrollToPosition.End, animate: false));
    }

    // ---------- Індикатор «ШІ думає» ----------

    private void SetWaiting(bool waiting)
    {
        _waiting = waiting;
        SendButton.IsEnabled = !waiting;
        TypingRow.IsVisible = waiting;
        if (waiting) _ = AnimateTypingAsync();
    }

    /// <summary>Крапки в тексті і підстрибування привида, поки чекаємо відповідь.</summary>
    private async Task AnimateTypingAsync()
    {
        var step = 0;
        while (_waiting)
        {
            TypingLabel.Text = "ШІ ДУМАЄ" + new string('.', step % 4);
            await TypingAvatar.TranslateTo(0, step % 2 == 0 ? -5 : 0, 200);
            step++;
            await Task.Delay(250);
        }
        TypingAvatar.TranslationY = 0;
    }

    // ---------- Очищення і вибір моделі ----------

    private void OnClearClicked(object? sender, EventArgs e)
    {
        if (_waiting) return;
        _lines.Clear();
        AddGreeting();
    }

    private void OnModelClicked(object? sender, EventArgs e) => ModelOverlay.IsVisible = true;
    private void OnModelCloseClicked(object? sender, EventArgs e) => ModelOverlay.IsVisible = false;

    private void OnModelPicked(object? sender, EventArgs e)
    {
        if ((sender as Button)?.BindingContext is string model) _ai.Model = model;
        UpdateModelButton();
        ModelOverlay.IsVisible = false;
    }

    private void UpdateModelButton()
    {
        // Показуємо коротку назву: без постачальника і без суфікса ":free"
        var name = _ai.Model.Split('/').Last().Replace(":free", "");
        ModelButton.Text = name.Length > 22 ? name[..22] + "…" : name;
    }

    // ---------- Ключ ----------

    private void OnKeySaveClicked(object? sender, EventArgs e)
    {
        var key = KeyEntry.Text?.Trim();
        if (string.IsNullOrEmpty(key)) return;
        _ai.SaveKey(key);
        KeyEntry.Text = "";
        KeyOverlay.IsVisible = false;
    }
}
