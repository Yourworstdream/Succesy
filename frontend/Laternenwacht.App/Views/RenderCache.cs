using System.Windows;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Zwischengespeicherte Darstellung für aufwendige, aber selten veränderte Flächen – vor allem Schatten.
/// WPF zeichnet das Element einmal in eine Bitmap und setzt danach nur noch diese Bitmap zusammen,
/// auch wenn daneben jede Sekunde Zahlen wechseln oder das Element verschoben, gedreht oder ausgeblendet wird.
/// Die Auflösung folgt der Bildschirm-DPI (auch beim Wechsel auf einen anderen Monitor), damit nichts verschwimmt.
/// </summary>
/// <example><c>&lt;Grid views:RenderCache.IsEnabled="True"&gt;…&lt;/Grid&gt;</c></example>
public static class RenderCache
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(RenderCache), new PropertyMetadata(false, OnIsEnabledChanged));

    private static readonly DependencyProperty SubscriptionProperty = DependencyProperty.RegisterAttached(
        "Subscription", typeof(DpiSubscription), typeof(RenderCache));

    public static bool GetIsEnabled(DependencyObject element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return (bool)element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject element, bool value)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            element.Loaded += OnLoaded;
            element.Unloaded += OnUnloaded;
            Apply(element);
        }
        else
        {
            element.Loaded -= OnLoaded;
            element.Unloaded -= OnUnloaded;
            element.CacheMode = null;
        }
    }

    private static void OnLoaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        Apply(element);

        if (element.GetValue(SubscriptionProperty) is null && Window.GetWindow(element) is { } window)
        {
            element.SetValue(SubscriptionProperty, new DpiSubscription(window, element));
        }
    }

    private static void OnUnloaded(object sender, RoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        (element.GetValue(SubscriptionProperty) as DpiSubscription)?.Cancel();
        element.ClearValue(SubscriptionProperty);
    }

    private static void Apply(FrameworkElement element)
    {
        var scale = VisualTreeHelper.GetDpi(element).DpiScaleX;
        if (element.CacheMode is BitmapCache current && Math.Abs(current.RenderAtScale - scale) < 0.01)
        {
            return;
        }

        var cache = new BitmapCache(scale) { SnapsToDevicePixels = true };
        cache.Freeze();
        element.CacheMode = cache;
    }

    /// <summary>Hört auf DPI-Wechsel des Fensters (Monitorwechsel) und lässt sich wieder abmelden.</summary>
    private sealed class DpiSubscription
    {
        private readonly Window _window;
        private readonly FrameworkElement _element;

        public DpiSubscription(Window window, FrameworkElement element)
        {
            _window = window;
            _element = element;
            _window.DpiChanged += OnDpiChanged;
        }

        public void Cancel() => _window.DpiChanged -= OnDpiChanged;

        private void OnDpiChanged(object sender, DpiChangedEventArgs e) => Apply(_element);
    }
}
