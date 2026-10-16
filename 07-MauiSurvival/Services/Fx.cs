namespace Survival.Services;

/// <summary>
/// Дрібні анімації, які роблять інтерфейс живим. Усі вони йдуть через вбудований рушій анімацій MAUI,
/// який працює в частоті оновлення екрана (60 і більше кадрів на секунду) і не навантажує код гри.
/// </summary>
public static class Fx
{
    /// <summary>Плавна поява екрана: з прозорості і невеликого зсуву вниз.</summary>
    public static async Task EnterAsync(ContentPage page)
    {
        if (page.Content is not { } content) return;
        content.Opacity = 0;
        content.TranslationY = 18;
        await Task.WhenAll(content.FadeTo(1, 260, Easing.CubicOut), content.TranslateTo(0, 0, 260, Easing.CubicOut));
    }

    /// <summary>Елементи з'являються по черзі (каскадом): кожен трохи пізніше за попередній.</summary>
    public static async Task StaggerAsync(IEnumerable<View> views, int stepMs = 70)
    {
        var list = views.ToList();
        var targets = list.ToDictionary(v => v, v => v.Opacity);   // запам'ятовуємо прозорість (вимкнені кнопки напівпрозорі)
        foreach (var v in list) { v.Opacity = 0; v.TranslationY = 24; }
        foreach (var v in list)
        {
            _ = Task.WhenAll(v.FadeTo(targets[v], 240, Easing.CubicOut), v.TranslateTo(0, 0, 280, Easing.CubicOut));
            await Task.Delay(stepMs);
        }
    }

    /// <summary>
    /// Напис, який виринає, летить угору і зникає (наприклад, «+3 ДЕРЕВО» або «Z» під час сну).
    /// Додається у шар-накладку, який лежить поверх екрана.
    /// </summary>
    public static async Task FloatTextAsync(Layout overlay, string text, Color color, double x, double y, double rise = 90, uint duration = 1100)
    {
        var label = new Label
        {
            Text = text,
            TextColor = color,
            FontFamily = "PixelFont",
            FontSize = 16,
            InputTransparent = true,
            Shadow = new Shadow { Brush = Color.FromArgb("#0F0F1A"), Offset = new Point(2, 2), Radius = 0, Opacity = 1 },
        };
        AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
        label.Scale = 0.6;
        overlay.Add(label);

        await Task.WhenAll(
            label.ScaleTo(1.15, 160, Easing.CubicOut),
            label.TranslateTo(0, -rise, duration, Easing.CubicOut),
            Task.Run(async () => { await Task.Delay((int)duration - 380); await MainThread.InvokeOnMainThreadAsync(() => label.FadeTo(0, 380)); }));

        overlay.Remove(label);
    }

    /// <summary>
    /// Частинка летить із поступовою появою і зникненням. Усе (позиція і прозорість) рахується в ОДНІЙ анімації,
    /// а не в трьох окремих, тому на кожен кадр іде втричі менше роботи. Після завершення частинка прибирається.
    /// </summary>
    public static void Fly(View particle, Layout layer, double dx, double dy, uint ms, double maxOpacity = 0.85, double wobble = 0)
    {
        var animation = new Animation(t =>
        {
            particle.TranslationX = dx * t + (wobble > 0 ? Math.Sin(t * 12.5) * wobble : 0);
            particle.TranslationY = dy * t;
            particle.Opacity = (t < 0.2 ? t / 0.2 : t > 0.75 ? (1 - t) / 0.25 : 1) * maxOpacity;
        });
        animation.Commit(particle, $"fly{particle.GetHashCode()}", 16, ms, Easing.Linear, (_, _) => layer.Remove(particle));
    }

    /// <summary>«Пульс»: елемент швидко збільшується і повертається назад.</summary>
    public static async Task PopAsync(VisualElement view, double scale = 1.25)
    {
        await view.ScaleTo(scale, 120, Easing.CubicOut);
        await view.ScaleTo(1, 180, Easing.CubicIn);
    }
}
