using System.Windows;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Leichtgewichtige Zeichenfläche aus mehreren übereinanderliegenden <see cref="DrawingVisual"/>-Ebenen
/// (Grundlage für Wegband, Faden, Wintermesser und Zierlinie).
/// </summary>
/// <remarks>
/// Jede Ebene wird nur dann neu gezeichnet, wenn sich ihr Inhalt wirklich ändert; was sich im Sekundentakt bewegt
/// (z. B. die Laterne auf dem Weg), wird gar nicht neu gezeichnet, sondern nur über eine vorhandene Transformation
/// verschoben. So entstehen pro Takt weder neue Zeichenbefehle noch Objekte. Ohne Vorlage, ohne Layout-Kinder.
/// </remarks>
internal sealed class DrawingLayers : FrameworkElement
{
    private readonly DrawingVisual[] _layers;
    private readonly double _height;
    private readonly Action _resized;

    /// <param name="count">Anzahl der Ebenen (Index 0 liegt ganz unten).</param>
    /// <param name="height">Gewünschte Höhe in Pixeln; die Breite folgt dem Platz, den das Layout zuteilt.</param>
    /// <param name="resized">Wird nach jeder Größenänderung aufgerufen (dann alle Ebenen neu zeichnen).</param>
    public DrawingLayers(int count, double height, Action resized)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        _height = height;
        _resized = resized ?? throw new ArgumentNullException(nameof(resized));
        _layers = new DrawingVisual[count];
        for (var i = 0; i < count; i++)
        {
            _layers[i] = new DrawingVisual();
            AddVisualChild(_layers[i]);
        }

        IsHitTestVisible = false;
        SnapsToDevicePixels = true;
    }

    /// <summary>Ebene mit dem angegebenen Index.</summary>
    public DrawingVisual this[int index] => _layers[index];

    protected override int VisualChildrenCount => _layers.Length;

    protected override Visual GetVisualChild(int index) => _layers[index];

    protected override Size MeasureOverride(Size availableSize) => new(0, _height);

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        _resized();
    }

    /// <summary>Hilfsfunktion für eingefrorene Volltonpinsel.</summary>
    public static SolidColorBrush Brush(byte a, byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        brush.Freeze();
        return brush;
    }

    /// <summary>Hilfsfunktion für eingefrorene Stifte.</summary>
    public static Pen Pen(Brush brush, double thickness, DashStyle? dash = null, PenLineCap cap = PenLineCap.Flat)
    {
        var pen = new Pen(brush, thickness) { StartLineCap = cap, EndLineCap = cap, DashCap = cap, LineJoin = PenLineJoin.Round };
        if (dash is not null)
        {
            pen.DashStyle = dash;
        }

        pen.Freeze();
        return pen;
    }

    /// <summary>Hilfsfunktion für eingefrorene Geometrien aus der Pfad-Kurzschreibweise.</summary>
    public static Geometry Geometry(string data)
    {
        var geometry = System.Windows.Media.Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }

    /// <summary>
    /// Sucht eine Geometrie im Thema (z. B. "Station3Geometry") und gibt sie eingefroren zurück –
    /// oder <c>null</c>, wenn es sie nicht gibt (etwa im Designer).
    /// </summary>
    public static Geometry? ThemeGeometry(string key)
    {
        if (Application.Current?.TryFindResource(key) is not Geometry geometry)
        {
            return null;
        }

        return geometry.IsFrozen ? geometry : (Geometry)geometry.GetCurrentValueAsFrozen();
    }
}
