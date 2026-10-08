using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Zierlinie zwischen Überschrift und Inhalt einer Pergamentkarte: feine Linie, Raute mit zwei Punkten in der Mitte,
/// feine Linie. Die Linien erscheinen in 45 % Deckkraft von <see cref="Ink"/>, Raute und Punkte voll.
/// </summary>
/// <remarks>Höhe 10 px, Breite vom Layout. Gezeichnet wird nur bei Größen- oder Farbänderung.</remarks>
public sealed class OrnamentDivider : UserControl
{
    /// <summary>Höhe der Zierlinie.</summary>
    public const double DividerHeight = 10;

    public static readonly DependencyProperty InkProperty = DependencyProperty.Register(
        nameof(Ink), typeof(Brush), typeof(OrnamentDivider),
        new PropertyMetadata(DrawingLayers.Brush(0xFF, 0x8A, 0x5A, 0x12), OnInkChanged));

    private static readonly Geometry Diamond = DrawingLayers.Geometry("M 15,0 L 20,5 L 15,10 L 10,5 Z");

    private readonly DrawingLayers _surface;

    public OrnamentDivider()
    {
        Focusable = false;
        IsTabStop = false;
        _surface = new DrawingLayers(1, DividerHeight, Draw);
        Content = _surface;
    }

    /// <summary>Farbe der Zierde (Standard: Goldtinte #8A5A12).</summary>
    public Brush Ink
    {
        get => (Brush)GetValue(InkProperty);
        set => SetValue(InkProperty, value);
    }

    private static void OnInkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((OrnamentDivider)d).Draw();

    private void Draw()
    {
        using var dc = _surface[0].RenderOpen();
        var width = _surface.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        var ink = Ink;
        var center = Math.Round(width / 2);
        var ornamentLeft = center - 15;

        // Linien links und rechts, 10 px Abstand zur Zierde (30 px breit)
        dc.PushOpacity(0.45);
        dc.DrawRectangle(ink, null, new Rect(0, 4.5, Math.Max(0, ornamentLeft - 10), 1));
        dc.DrawRectangle(ink, null, new Rect(ornamentLeft + 40, 4.5, Math.Max(0, width - ornamentLeft - 40), 1));
        dc.Pop();

        dc.PushTransform(new TranslateTransform(ornamentLeft, 0));
        dc.DrawGeometry(ink, null, Diamond);
        dc.DrawEllipse(ink, null, new Point(4, 5), 1.5, 1.5);
        dc.DrawEllipse(ink, null, new Point(26, 5), 1.5, 1.5);
        dc.Pop();
    }
}
