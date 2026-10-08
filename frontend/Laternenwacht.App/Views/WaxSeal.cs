using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Wachssiegel: gewellter Wachsrand, eingeprägte Scheibe und ein Zeichen in der Mitte
/// (Laterne für die intakte Chronik, Blatt für Lob, Schneeflocke für Mahnrufe …).
/// </summary>
/// <remarks>
/// Zeichnet direkt, ohne Vorlage und ohne Effekte. Die Farbe kommt aus <see cref="SealBrush"/>; Licht und Schatten
/// der Prägung entstehen aus halbtransparentem Weiß bzw. Schwarz darüber, sodass jede Wachsfarbe passt
/// (Narnia-Rot <c>NarniaRedBrush</c>, Frühlingsgrün <c>SpringSealBrush</c> …). Neu gezeichnet wird nur, wenn sich
/// eine Eigenschaft ändert.
/// </remarks>
public sealed class WaxSeal : Control
{
    /// <summary>Kantenlänge des Entwurfsrasters, in dem die Siegelform beschrieben ist.</summary>
    private const double Grid = 44;

    public static readonly DependencyProperty SealBrushProperty = DependencyProperty.Register(
        nameof(SealBrush), typeof(Brush), typeof(WaxSeal),
        new FrameworkPropertyMetadata(Frozen(Color.FromRgb(0x9E, 0x2B, 0x1F)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GlyphProperty = DependencyProperty.Register(
        nameof(Glyph), typeof(Geometry), typeof(WaxSeal),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
        nameof(Diameter), typeof(double), typeof(WaxSeal),
        new FrameworkPropertyMetadata(44d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender),
        value => value is double d && d >= 0 && !double.IsInfinity(d));

    public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.Register(
        nameof(GlyphBrush), typeof(Brush), typeof(WaxSeal),
        new FrameworkPropertyMetadata(Frozen(Color.FromArgb(0xE6, 0xF6, 0xE0, 0xD6)), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsGlyphFilledProperty = DependencyProperty.Register(
        nameof(IsGlyphFilled), typeof(bool), typeof(WaxSeal),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Gewellter Wachsrand im 44er-Raster (gleiche Form wie <c>WaxSealGeometry</c> im Thema).</summary>
    private static readonly Geometry Rim = FrozenGeometry(
        "M 38.63,28.89 Q 37.56,37.56 28.89,38.63 Q 22,44 15.11,38.63 Q 6.44,37.56 5.37,28.89 Q 0,22 5.37,15.11 " +
        "Q 6.44,6.44 15.11,5.37 Q 22,0 28.89,5.37 Q 37.56,6.44 38.63,15.11 Q 44,22 38.63,28.89 Z");

    private static readonly Brush Highlight = Frozen(Color.FromArgb(0x1F, 0xFF, 0xFF, 0xFF));
    private static readonly Pen Embossing = FrozenPen(Color.FromArgb(0x59, 0x00, 0x00, 0x00), 1.4);
    private static readonly Pen InnerGroove = FrozenPen(Color.FromArgb(0x33, 0x00, 0x00, 0x00), 0.6);

    static WaxSeal()
    {
        FocusableProperty.OverrideMetadata(typeof(WaxSeal), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(WaxSeal), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Farbe des Wachses (Standard: Narnia-Rot).</summary>
    public Brush SealBrush
    {
        get => (Brush)GetValue(SealBrushProperty);
        set => SetValue(SealBrushProperty, value);
    }

    /// <summary>
    /// Zeichen in der Mitte, z. B. <c>SealLanternGeometry</c>, <c>LeafGeometry</c> oder <c>CrossGeometry</c>.
    /// Es wird in die Prägung eingepasst (Seitenverhältnis bleibt) und als Linie gezeichnet,
    /// bei <see cref="IsGlyphFilled"/> gefüllt.
    /// </summary>
    public Geometry? Glyph
    {
        get => (Geometry?)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>Durchmesser in Pixeln (Standard 44).</summary>
    public double Diameter
    {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    /// <summary>Farbe des Zeichens (Standard: helles, leicht rosiges Elfenbein).</summary>
    public Brush GlyphBrush
    {
        get => (Brush)GetValue(GlyphBrushProperty);
        set => SetValue(GlyphBrushProperty, value);
    }

    /// <summary><c>true</c> = Zeichen gefüllt (z. B. Laterne), <c>false</c> = als Linie (Standard).</summary>
    public bool IsGlyphFilled
    {
        get => (bool)GetValue(IsGlyphFilledProperty);
        set => SetValue(IsGlyphFilledProperty, value);
    }

    protected override Size MeasureOverride(Size constraint) => new(Diameter, Diameter);

    protected override Size ArrangeOverride(Size arrangeBounds) => arrangeBounds;

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        var size = Math.Min(Diameter, Math.Min(ActualWidth, ActualHeight));
        if (size <= 0)
        {
            return;
        }

        var scale = size / Grid;
        var offsetX = (ActualWidth - size) / 2;
        var offsetY = (ActualHeight - size) / 2;
        drawingContext.PushTransform(new MatrixTransform(scale, 0, 0, scale, offsetX, offsetY));

        // Wachsrand, eingeprägte Scheibe (etwas heller), feine Rille
        var center = new Point(Grid / 2, Grid / 2);
        drawingContext.DrawGeometry(SealBrush, null, Rim);
        drawingContext.DrawEllipse(SealBrush, null, center, 12.5, 12.5);
        drawingContext.DrawEllipse(Highlight, Embossing, center, 12.5, 12.5);
        drawingContext.DrawEllipse(null, InnerGroove, center, 10, 10);

        if (Glyph is { } glyph && !glyph.Bounds.IsEmpty && glyph.Bounds.Width > 0 && glyph.Bounds.Height > 0)
        {
            DrawGlyph(drawingContext, glyph, center);
        }

        drawingContext.Pop();
    }

    /// <summary>Passt das Zeichen in ein Quadrat von 13 Einheiten in der Siegelmitte ein.</summary>
    private void DrawGlyph(DrawingContext drawingContext, Geometry glyph, Point center)
    {
        const double box = 13;
        var bounds = glyph.Bounds;
        var fit = box / Math.Max(bounds.Width, bounds.Height);
        var x = center.X - (bounds.Width * fit / 2) - (bounds.X * fit);
        var y = center.Y - (bounds.Height * fit / 2) - (bounds.Y * fit);
        drawingContext.PushTransform(new MatrixTransform(fit, 0, 0, fit, x, y));

        if (IsGlyphFilled)
        {
            drawingContext.DrawGeometry(GlyphBrush, null, glyph);
        }
        else
        {
            // Strichstärke 1,5 in Siegel-Einheiten, unabhängig von der Größe des Zeichens
            var pen = new Pen(GlyphBrush, 1.5 / fit) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            pen.Freeze();
            drawingContext.DrawGeometry(null, pen, glyph);
        }

        drawingContext.Pop();
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(Color color, double thickness)
    {
        var pen = new Pen(Frozen(color), thickness);
        pen.Freeze();
        return pen;
    }

    private static Geometry FrozenGeometry(string data)
    {
        var geometry = Geometry.Parse(data);
        geometry.Freeze();
        return geometry;
    }
}
