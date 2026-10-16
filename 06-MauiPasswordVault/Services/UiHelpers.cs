namespace PassVault.Services;

/// <summary>Маленькі помічники для побудови однакових елементів інтерфейсу з коду.</summary>
public static class UiHelpers
{
    /// <summary>«Чіп» категорії: кнопка з іконкою; обрана підсвічується золотим.</summary>
    public static Button MakeChip(string text, string icon, bool selected, object context, EventHandler onClick)
    {
        var res = Application.Current!.Resources;
        var chip = new Button
        {
            Text = text,
            ImageSource = icon,
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8),
            Style = (Style)res["PixelButton"],
            FontSize = 9,
            Padding = new Thickness(12, 8),
            HeightRequest = 52,
            BindingContext = context,
            BackgroundColor = selected ? (Color)res["Gold"] : (Color)res["Panel"],
            TextColor = selected ? (Color)res["Ink"] : (Color)res["Paper"],
        };
        chip.Clicked += onClick;
        return chip;
    }
}
