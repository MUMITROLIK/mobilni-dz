namespace Survival.Controls;

/// <summary>
/// Живий фон усіх екранів: нічне небо, зірки, що мерехтять, і хмаринки, що пливуть.
///
/// Про швидкість: анімація кожного окремого елемента коштує процесорного часу на кожен кадр.
/// Тому мерехтіння зроблено не 26 окремими зірками, а двома прозорими картинками із зірками:
/// вони по черзі плавно проявляються і гаснуть. Це всього дві анімації замість двох десятків,
/// а виглядає так само живо.
/// </summary>
public class Backdrop : ContentView
{
    private readonly Image _starsA = new() { Source = "stars_a.png", Aspect = Aspect.AspectFill, Opacity = 1, InputTransparent = true };
    private readonly Image _starsB = new() { Source = "stars_b.png", Aspect = Aspect.AspectFill, Opacity = 0.1, InputTransparent = true };
    private readonly AbsoluteLayout _clouds = new() { InputTransparent = true };
    private readonly List<(Image Cloud, double Y, uint Seconds)> _cloudList = new();
    private CancellationTokenSource? _cts;

    public Backdrop()
    {
        var root = new Grid { InputTransparent = true, CascadeInputTransparent = true };
        root.Add(new Image { Source = "bg_night.png", Aspect = Aspect.AspectFill });
        root.Add(_starsA);
        root.Add(_starsB);
        root.Add(_clouds);
        Content = root;
        InputTransparent = true;

        // Лише дві хмаринки різної висоти і швидкості: цього досить для відчуття глибини
        foreach (var (y, seconds, opacity) in new[] { (0.12, 80u, 0.28), (0.34, 120u, 0.18) })
        {
            var cloud = new Image { Source = "cloud.png", WidthRequest = 160, HeightRequest = 160, Opacity = opacity, InputTransparent = true };
            AbsoluteLayout.SetLayoutBounds(cloud, new Rect(0, 0, 160, 160));
            _clouds.Add(cloud);
            _cloudList.Add((cloud, y, seconds));
        }

        Loaded += (_, _) => Start();
        Unloaded += (_, _) => _cts?.Cancel();
    }

    private void Start()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

#if WINDOWS
        // На Windows анімації віддаємо системному композитору (GPU): вони не навантажують потік інтерфейсу
        // і йдуть рівно з частотою екрана, навіть коли в грі багато іншого відбувається.
        if (StartCompositionAnimations()) return;
#endif
        _ = TwinkleAsync(token);
        foreach (var (cloud, y, seconds) in _cloudList) _ = DriftAsync(cloud, y, seconds, token);
    }

#if WINDOWS
    /// <summary>
    /// Запускає анімації прямо в композиторі WinUI (Microsoft.UI.Composition): прозорість шарів зірок
    /// і зсув хмаринок рахує відеопідсистема, а не код застосунку. Повертає false, якщо не вийшло.
    /// </summary>
    private bool StartCompositionAnimations()
    {
        try
        {
            if (_starsA.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement a ||
                _starsB.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement b) return false;

            // Мерехтіння: перший шар спалахує, поки другий гасне, і навпаки (Direction = Alternate)
            LoopScalar(a, "Opacity", 1f, 0.15f, 5200);
            LoopScalar(b, "Opacity", 0.15f, 1f, 5200);

            // Хмаринки пливуть зліва направо: анімується горизонтальне зміщення візуала
            var width = (float)(Width > 0 ? Width : 1280);
            foreach (var (cloud, y, seconds) in _cloudList)
            {
                if (cloud.Handler?.PlatformView is not Microsoft.UI.Xaml.UIElement el) return false;
                cloud.TranslationY = Height * y;
                LoopScalar(el, "Offset.X", -200f, width + 40f, (int)seconds * 1000, alternate: false);
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void LoopScalar(Microsoft.UI.Xaml.UIElement element, string property, float from, float to, int milliseconds, bool alternate = true)
    {
        var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
        var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
        animation.InsertKeyFrame(0f, from);
        animation.InsertKeyFrame(1f, to, visual.Compositor.CreateLinearEasingFunction());
        animation.Duration = TimeSpan.FromMilliseconds(milliseconds);
        animation.IterationBehavior = Microsoft.UI.Composition.AnimationIterationBehavior.Forever;
        animation.Direction = alternate ? Microsoft.UI.Composition.AnimationDirection.Alternate : Microsoft.UI.Composition.AnimationDirection.Normal;
        visual.StartAnimation(property, animation);
    }
#endif

    /// <summary>Два шари зірок по черзі проявляються: одні гаснуть, поки інші спалахують.</summary>
    private async Task TwinkleAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await Task.WhenAll(_starsA.FadeTo(0.15, 2600, Easing.SinInOut), _starsB.FadeTo(1, 2600, Easing.SinInOut));
            await Task.WhenAll(_starsA.FadeTo(1, 2600, Easing.SinInOut), _starsB.FadeTo(0.15, 2600, Easing.SinInOut));
        }
    }

    /// <summary>Хмаринка повільно пливе зліва направо і виринає знову ліворуч.</summary>
    private async Task DriftAsync(Image cloud, double yFraction, uint seconds, CancellationToken token)
    {
        await Task.Delay(100);
        while (!token.IsCancellationRequested)
        {
            var width = Width > 0 ? Width : 1200;
            cloud.TranslationY = Height * yFraction;
            cloud.TranslationX = -200;
            await cloud.TranslateTo(width + 40, cloud.TranslationY, seconds * 1000, Easing.Linear);
        }
    }
}
