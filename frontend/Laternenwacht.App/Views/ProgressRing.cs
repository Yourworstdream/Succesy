using System.Windows;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Kreisförmiger Fortschrittsring mit abgerundeten Enden – das zentrale Formelement
/// des Designs (Hauptansicht und Fokusleiste). Zeichnet direkt, ohne Vorlagen.
/// Der Lichtschein (<see cref="Glow"/>) entsteht aus zwei breiteren, blassen Strichen statt aus einem
/// Schatteneffekt: Effekte werden bei jeder Änderung als Pixel-Shader neu berechnet – der Ring ändert sich jede Sekunde.
/// </summary>
public sealed class ProgressRing : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(Brushes.OrangeRed, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty GlowProperty = DependencyProperty.Register(
        nameof(Glow), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Fortschritt von 0 bis 1.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush RingBrush
    {
        get => (Brush)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    /// <summary>Breite des Lichtscheins um den Fortschrittsbogen (0 = kein Schein).</summary>
    public double Glow
    {
        get => (double)GetValue(GlowProperty);
        set => SetValue(GlowProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        var size = Math.Min(ActualWidth, ActualHeight);
        if (size <= Thickness)
        {
            return;
        }

        var center = new Point(ActualWidth / 2, ActualHeight / 2);
        var radius = (size - Thickness) / 2;
        drawingContext.DrawEllipse(null, new Pen(TrackBrush, Thickness), center, radius, radius);

        var progress = double.IsNaN(Progress) ? 0 : Math.Clamp(Progress, 0, 1);
        if (progress <= 0)
        {
            return;
        }

        Geometry arc;
        if (progress >= 0.9999)
        {
            arc = new EllipseGeometry(center, radius, radius);
        }
        else
        {
            var angle = progress * 360;
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(PointOnCircle(center, radius, 0), isFilled: false, isClosed: false);
                context.ArcTo(PointOnCircle(center, radius, angle), new Size(radius, radius), 0,
                    isLargeArc: angle > 180, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
            }

            arc = geometry;
        }

        arc.Freeze();

        if (Glow > 0)
        {
            DrawStroke(drawingContext, arc, Thickness + (Glow * 2), opacity: 0.10);
            DrawStroke(drawingContext, arc, Thickness + Glow, opacity: 0.22);
        }

        DrawStroke(drawingContext, arc, Thickness, opacity: 1);
    }

    private void DrawStroke(DrawingContext drawingContext, Geometry arc, double thickness, double opacity)
    {
        var pen = new Pen(RingBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if (opacity < 1)
        {
            drawingContext.PushOpacity(opacity);
        }

        drawingContext.DrawGeometry(null, pen, arc);
        if (opacity < 1)
        {
            drawingContext.Pop();
        }
    }

    /// <summary>Punkt auf dem Kreis; 0° liegt oben (12 Uhr), gezählt im Uhrzeigersinn.</summary>
    private static Point PointOnCircle(Point center, double radius, double degrees)
    {
        var radians = (degrees - 90) * Math.PI / 180;
        return new Point(center.X + (radius * Math.Cos(radians)), center.Y + (radius * Math.Sin(radians)));
    }
}
