namespace Survival.Services;

/// <summary>Маленькі помічники для побудови однакових елементів інтерфейсу з коду.</summary>
public static class UiHelpers
{
    public static Color Res(string key) => (Color)Application.Current!.Resources[key];

    /// <summary>Коротке повідомлення-підказка: з'являється на 2 секунди і зникає.</summary>
    public static async Task ToastAsync(Label label, string text)
    {
        label.Text = text;
        label.IsVisible = true;
        var shown = text;
        await Task.Delay(2200);
        if (label.Text == shown) label.IsVisible = false;   // не ховаємо, якщо вже показали новіше повідомлення
    }

    /// <summary>Піксельна «картка» з рамкою: контейнер для рядків списків.</summary>
    public static Border Card(View content, Color? background = null, double padding = 14) => new()
    {
        Content = content,
        BackgroundColor = background ?? Res("Panel"),
        Stroke = Res("Ink"),
        StrokeThickness = 4,
        StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
        Padding = padding,
    };

    /// <summary>Кнопка в піксельному стилі з готовим стилем із Pixel.xaml.</summary>
    public static Button PixelButton(string text, EventHandler onClick, string style = "PixelButton", double fontSize = 10)
    {
        var button = new Button { Text = text, Style = (Style)Application.Current!.Resources[style], FontSize = fontSize };
        button.Clicked += onClick;
        return button;
    }
}
