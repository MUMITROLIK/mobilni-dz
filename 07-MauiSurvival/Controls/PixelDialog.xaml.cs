namespace Survival.Controls;

/// <summary>
/// Власне діалогове вікно в піксельному стилі замість стандартного DisplayAlert.
/// Використання: <c>var ok = await Dialog.ShowAsync("Заголовок", "Текст", "ТАК", "НІ");</c>
/// Результат: true - натиснули першу кнопку, false - другу.
/// </summary>
public partial class PixelDialog : ContentView
{
    private TaskCompletionSource<bool>? _result;

    public PixelDialog()
    {
        InitializeComponent();
    }

    public async Task<bool> ShowAsync(string title, string message, string accept, string? cancel = null)
    {
        TitleLabel.Text = title;
        MessageLabel.Text = message;
        AcceptButton.Text = accept;

        // Якщо друга кнопка не потрібна, розтягуємо першу на всю ширину
        CancelButton.IsVisible = cancel is not null;
        CancelButton.Text = cancel ?? "";
        Grid.SetColumnSpan(AcceptButton, cancel is null ? 2 : 1);

        _result = new TaskCompletionSource<bool>();
        Panel.Scale = 0.85;
        IsVisible = true;
        _ = Panel.ScaleTo(1, 120, Easing.CubicOut);   // маленька поява "стрибком"

        return await _result.Task;
    }

    private void Close(bool value)
    {
        IsVisible = false;
        _result?.TrySetResult(value);
    }

    private void OnAcceptClicked(object? sender, EventArgs e) => Close(true);
    private void OnCancelClicked(object? sender, EventArgs e) => Close(false);
}
