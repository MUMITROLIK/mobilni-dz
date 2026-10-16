namespace Survival.Controls;

/// <summary>
/// Піксельна смуга-індикатор. Заповнення - один суцільний блок, який плавно розтягується (ScaleX),
/// а поверх нього лежать темні роздільники, через які смуга виглядає складеною з окремих «пікселів».
/// Завдяки цьому зміна значення йде плавно, а не стрибками по блоках.
/// </summary>
public class PixelBar : ContentView
{
    private readonly BoxView _fill = new() { AnchorX = 0, ScaleX = 0, HorizontalOptions = LayoutOptions.Fill, CornerRadius = 0 };
    private double _target;
    private double _shown;

    public PixelBar(int segments = 20, double height = 14)
    {
        var holder = new Grid { HeightRequest = height };
        holder.Add(_fill);

        // Роздільники між блоками
        var dividers = new Grid { InputTransparent = true, ColumnSpacing = 0 };
        for (var i = 0; i < segments; i++)
        {
            dividers.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            dividers.Add(new BoxView { Color = Color.FromArgb("#0F0F1A"), WidthRequest = 2, HorizontalOptions = LayoutOptions.End, InputTransparent = true }, i, 0);
        }
        holder.Add(dividers);

        HorizontalOptions = LayoutOptions.Fill;
        Content = new Border
        {
            Content = holder,
            Padding = 3,
            BackgroundColor = Color.FromArgb("#1A1C2C"),
            Stroke = Color.FromArgb("#0F0F1A"),
            StrokeThickness = 3,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle(),
        };
    }

    /// <summary>Задає цільове значення і колір. Сама смуга доїде до нього плавно (метод Step).</summary>
    public void Set(double value, double max, Color fill)
    {
        _target = Math.Clamp(value / max, 0, 1);
        _fill.Color = fill;
    }

    /// <summary>Крок анімації: наближає показане значення до цільового. Викликається ~60 разів на секунду.</summary>
    public void Step(double speed = 0.16)
    {
        if (Math.Abs(_target - _shown) < 0.0005)
        {
            if (_shown != _target) { _shown = _target; _fill.ScaleX = _shown; }
            return;
        }
        _shown += (_target - _shown) * speed;
        _fill.ScaleX = _shown;
    }
}
