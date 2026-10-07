using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Laternenwacht.App.Views;

/// <summary>
/// Goldgerahmtes Szenenbild der Reise: oben als Bogen (halbe Ellipse, wie ein Kirchenfenster) oder als
/// abgerundetes Rechteck, mit doppeltem Goldrahmen und großem Schatten. Über dem Bild liegen drei Stimmungsschleier –
/// eisige Vignette (<see cref="FrostLevel"/>), warmer Rastschein von unten (<see cref="WarmLevel"/>) und Abdunkelung
/// (<see cref="DimLevel"/>) – und darüber der <see cref="ContentControl.Content"/> (z. B. Restzeit und Erzähltext).
/// </summary>
/// <remarks>
/// Die Vorlage steht im Thema (Laternendickicht.xaml, Stil für <c>views:SceneFrame</c>). Form, Rahmen und äußerer Ring
/// werden nur bei Größenänderung bzw. beim Umschalten von <see cref="IsArched"/> neu berechnet und eingefroren;
/// der Schatten liegt allein in einer zwischengespeicherten Ebene (<see cref="RenderCache"/>), Gold- und Eisring daneben,
/// damit das Überblenden mit <see cref="FrostLevel"/> den Schatten nicht neu rastert. Die Ringe stehen in einem Canvas,
/// weil sie über das Element hinausragen und ein Grid sie sonst abschneiden würde. Gibt es Inhalt, dunkelt ein
/// Verlauf das untere Bilddrittel für die Lesbarkeit ab. Bilder kommen aus der <see cref="SceneLibrary"/>.
/// </remarks>
public class SceneFrame : ContentControl
{
    /// <summary>Radius der unteren Ecken (bzw. aller Ecken ohne Bogen).</summary>
    private const double CornerRadius = 18;

    /// <summary>Höhe des Bogens im Verhältnis zur Breite (entspricht border-radius 250px/210px im Entwurf).</summary>
    private const double ArchRatio = 0.42;

    /// <summary>Abstand der äußeren Goldlinie vom Bild (dunkle Fuge 5 px, Linie 1 px).</summary>
    private const double RingOffset = 5.5;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(SceneFrame), new PropertyMetadata(null));

    public static readonly DependencyProperty IsArchedProperty = DependencyProperty.Register(
        nameof(IsArched), typeof(bool), typeof(SceneFrame), new PropertyMetadata(true, OnShapeChanged));

    public static readonly DependencyProperty FrostLevelProperty = DependencyProperty.Register(
        nameof(FrostLevel), typeof(double), typeof(SceneFrame), new PropertyMetadata(0d, OnFrostLevelChanged, (_, value) => CoerceLevel(value)));

    public static readonly DependencyProperty WarmLevelProperty = DependencyProperty.Register(
        nameof(WarmLevel), typeof(double), typeof(SceneFrame), new PropertyMetadata(0d, null, (_, value) => CoerceLevel(value)));

    public static readonly DependencyProperty DimLevelProperty = DependencyProperty.Register(
        nameof(DimLevel), typeof(double), typeof(SceneFrame), new PropertyMetadata(0d, null, (_, value) => CoerceLevel(value)));

    private static readonly DependencyPropertyKey ShapeGeometryKey = DependencyProperty.RegisterReadOnly(
        nameof(ShapeGeometry), typeof(Geometry), typeof(SceneFrame), new PropertyMetadata(Geometry.Empty));

    private static readonly DependencyPropertyKey FrameGeometryKey = DependencyProperty.RegisterReadOnly(
        nameof(FrameGeometry), typeof(Geometry), typeof(SceneFrame), new PropertyMetadata(Geometry.Empty));

    private static readonly DependencyPropertyKey OuterRingGeometryKey = DependencyProperty.RegisterReadOnly(
        nameof(OuterRingGeometry), typeof(Geometry), typeof(SceneFrame), new PropertyMetadata(Geometry.Empty));

    private static readonly DependencyPropertyKey ShadeHeightKey = DependencyProperty.RegisterReadOnly(
        nameof(ShadeHeight), typeof(double), typeof(SceneFrame), new PropertyMetadata(0d));

    /// <summary>Umriss des Bildes (dient als Beschnitt).</summary>
    public static readonly DependencyProperty ShapeGeometryProperty = ShapeGeometryKey.DependencyProperty;

    /// <summary>Mittellinie des 2 px breiten goldenen Innenrahmens.</summary>
    public static readonly DependencyProperty FrameGeometryProperty = FrameGeometryKey.DependencyProperty;

    /// <summary>Äußerer Ring (dunkle Fuge mit feiner Goldlinie), ragt 6 px über das Element hinaus.</summary>
    public static readonly DependencyProperty OuterRingGeometryProperty = OuterRingGeometryKey.DependencyProperty;

    /// <summary>Höhe des Lesbarkeitsverlaufs (40 % der Bildhöhe).</summary>
    public static readonly DependencyProperty ShadeHeightProperty = ShadeHeightKey.DependencyProperty;

    private Shape? _goldRing;

    /// <summary>Das Szenenbild, in der Regel aus der <see cref="SceneLibrary"/>.</summary>
    public ImageSource? Source
    {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary><c>true</c> = Bogen oben (laufende Wacht, Ende), <c>false</c> = abgerundetes Rechteck (bereit).</summary>
    public bool IsArched
    {
        get => (bool)GetValue(IsArchedProperty);
        set => SetValue(IsArchedProperty, value);
    }

    /// <summary>Eisige Vignette und Eisring, 0 bis 1 (Ablenkung).</summary>
    public double FrostLevel
    {
        get => (double)GetValue(FrostLevelProperty);
        set => SetValue(FrostLevelProperty, value);
    }

    /// <summary>Warmer Schein von unten, 0 bis 1 (Rast).</summary>
    public double WarmLevel
    {
        get => (double)GetValue(WarmLevelProperty);
        set => SetValue(WarmLevelProperty, value);
    }

    /// <summary>Abdunkelung, 0 bis 1 (Abwesenheit).</summary>
    public double DimLevel
    {
        get => (double)GetValue(DimLevelProperty);
        set => SetValue(DimLevelProperty, value);
    }

    /// <inheritdoc cref="ShapeGeometryProperty"/>
    public Geometry ShapeGeometry => (Geometry)GetValue(ShapeGeometryProperty);

    /// <inheritdoc cref="FrameGeometryProperty"/>
    public Geometry FrameGeometry => (Geometry)GetValue(FrameGeometryProperty);

    /// <inheritdoc cref="OuterRingGeometryProperty"/>
    public Geometry OuterRingGeometry => (Geometry)GetValue(OuterRingGeometryProperty);

    /// <inheritdoc cref="ShadeHeightProperty"/>
    public double ShadeHeight => (double)GetValue(ShadeHeightProperty);

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _goldRing = GetTemplateChild("PART_GoldRing") as Shape;
        UpdateRings();
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateShape();
    }

    /// <summary>Umriss für ein Rechteck: Bogen oben (halbe Ellipse über die ganze Breite) oder ringsum gerundet.</summary>
    /// <param name="bounds">Rechteck, das der Umriss ausfüllt.</param>
    /// <param name="arched">Bogen oben?</param>
    /// <param name="archHeight">Höhe des Bogens (senkrechter Radius).</param>
    /// <param name="corner">Radius der unteren Ecken.</param>
    internal static Geometry BuildShape(Rect bounds, bool arched, double archHeight, double corner)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return Geometry.Empty;
        }

        corner = Math.Clamp(corner, 0, Math.Min(bounds.Width, bounds.Height) / 2);
        Geometry geometry;
        if (!arched)
        {
            geometry = new RectangleGeometry(bounds, corner, corner);
        }
        else
        {
            var radiusX = bounds.Width / 2;
            var radiusY = Math.Clamp(archHeight, 0, Math.Max(0, bounds.Height - corner));
            var stream = new StreamGeometry();
            using (var context = stream.Open())
            {
                context.BeginFigure(new Point(bounds.Left, bounds.Top + radiusY), isFilled: true, isClosed: true);
                context.ArcTo(new Point(bounds.Right, bounds.Top + radiusY), new Size(radiusX, radiusY), 0,
                    isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
                context.LineTo(new Point(bounds.Right, bounds.Bottom - corner), isStroked: true, isSmoothJoin: true);
                context.ArcTo(new Point(bounds.Right - corner, bounds.Bottom), new Size(corner, corner), 0,
                    isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
                context.LineTo(new Point(bounds.Left + corner, bounds.Bottom), isStroked: true, isSmoothJoin: true);
                context.ArcTo(new Point(bounds.Left, bounds.Bottom - corner), new Size(corner, corner), 0,
                    isLargeArc: false, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
            }

            geometry = stream;
        }

        geometry.Freeze();
        return geometry;
    }

    private static double CoerceLevel(object baseValue) =>
        baseValue is double value && !double.IsNaN(value) ? Math.Clamp(value, 0, 1) : 0;

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SceneFrame)d).UpdateShape();

    private static void OnFrostLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((SceneFrame)d).UpdateRings();

    private void UpdateShape()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        var arched = IsArched;
        var archHeight = width * ArchRatio;
        var bounds = new Rect(0, 0, width, height);

        SetValue(ShapeGeometryKey, BuildShape(bounds, arched, archHeight, CornerRadius));

        var frame = bounds;
        frame.Inflate(-1, -1);
        SetValue(FrameGeometryKey, BuildShape(frame, arched, archHeight - 1, CornerRadius - 1));

        var ring = bounds;
        ring.Inflate(RingOffset, RingOffset);
        SetValue(OuterRingGeometryKey, BuildShape(ring, arched, archHeight + RingOffset, CornerRadius + RingOffset));

        SetValue(ShadeHeightKey, Math.Round(height * 0.4));
    }

    /// <summary>Goldring und Eisring blenden gegeneinander über; der Eisring folgt <see cref="FrostLevel"/> in der Vorlage.</summary>
    private void UpdateRings()
    {
        if (_goldRing is not null)
        {
            _goldRing.Opacity = 1 - FrostLevel;
        }
    }
}
