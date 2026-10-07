using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Das Wegband der Reise auf der Pergamentkarte: eine punktierte Linie mit zehn Medaillons
/// (Laternenpfahl … Cair Paravel), der goldenen Spur bis zur Laterne, den Stücken Türkischen Honigs darüber
/// und – bei Ablenkung – dem gestrichelten "Abweg" zum Schlitten der Königin.
/// </summary>
/// <remarks>
/// <para>Medaillon k sitzt bei (k−1)/9 der Breite. Erreichte Stationen sind goldgefüllt, die aktuelle ist größer und
/// leuchtet, offene haben einen gestrichelten Ring. Die Laterne steht bei <see cref="Progress"/>.</para>
/// <para>Sparsam: Das Band besteht aus sechs Zeichenebenen (<see cref="DrawingLayers"/>). Neu gezeichnet wird eine Ebene
/// nur, wenn sich ihre Eigenschaft ändert (Station, Honig, Frost/Rast/Krone) oder die Größe. Der Fortschritt verschiebt
/// lediglich die vorhandenen Transformationen der Spur und der Laterne – auf ganze Pixel gerundet, sodass sich im
/// Sekundentakt meist gar nichts ändert.</para>
/// <para>Höhe 68 px; die Breite gibt das Layout vor. Laterne, Krone und Schlitten dürfen oben etwas überstehen.</para>
/// </remarks>
public sealed class JourneyBand : UserControl
{
    /// <summary>Gesamthöhe des Bandes.</summary>
    public const double BandHeight = 68;

    private const double Inset = 16;
    private const double TrackY = 48;
    private const int StationCount = 10;

    private const int TrackLayer = 0;
    private const int TrailLayer = 1;
    private const int TickLayer = 2;
    private const int MedallionLayer = 3;
    private const int HoneyLayer = 4;
    private const int MarkerLayer = 5;

    public static readonly DependencyProperty StationProperty = DependencyProperty.Register(
        nameof(Station), typeof(int), typeof(JourneyBand), new PropertyMetadata(1, OnStationChanged, (_, value) => CoerceStation(value)));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(JourneyBand), new PropertyMetadata(0d, OnProgressChanged, (_, value) => CoerceFraction(value)));

    public static readonly DependencyProperty HoneyMarksProperty = DependencyProperty.Register(
        nameof(HoneyMarks), typeof(IEnumerable<double>), typeof(JourneyBand), new PropertyMetadata(null, OnHoneyMarksChanged));

    public static readonly DependencyProperty IsFrostProperty = DependencyProperty.Register(
        nameof(IsFrost), typeof(bool), typeof(JourneyBand), new PropertyMetadata(false, OnMarkerChanged));

    public static readonly DependencyProperty IsRestingProperty = DependencyProperty.Register(
        nameof(IsResting), typeof(bool), typeof(JourneyBand), new PropertyMetadata(false, OnMarkerChanged));

    public static readonly DependencyProperty ShowCrownProperty = DependencyProperty.Register(
        nameof(ShowCrown), typeof(bool), typeof(JourneyBand), new PropertyMetadata(false, OnMarkerChanged));

    // ===== Farben aus dem Entwurf (Tinte auf Pergament) =====
    private static readonly Brush Ink = DrawingLayers.Brush(0xFF, 0x2A, 0x1E, 0x14);
    private static readonly Brush InkFaint = DrawingLayers.Brush(0x66, 0x2A, 0x1E, 0x14);
    private static readonly Brush Gold = DrawingLayers.Brush(0xFF, 0xE7, 0xB7, 0x5F);
    private static readonly Brush GoldDeep = DrawingLayers.Brush(0xFF, 0xC8, 0x91, 0x3A);
    private static readonly Brush InkGold = DrawingLayers.Brush(0xFF, 0x8A, 0x5A, 0x12);
    private static readonly Brush Paper = DrawingLayers.Brush(0xFF, 0xF6, 0xEE, 0xDA);
    private static readonly Brush Ice = DrawingLayers.Brush(0xFF, 0x4F, 0x92, 0xB5);
    private static readonly Brush IceInk = DrawingLayers.Brush(0xFF, 0x2C, 0x6A, 0x8A);
    private static readonly Brush IceWindow = DrawingLayers.Brush(0xFF, 0xCF, 0xEA, 0xF7);
    private static readonly Brush WarmWindow = DrawingLayers.Brush(0xFF, 0xFF, 0xF0, 0xC4);
    private static readonly Brush HoneyFill = DrawingLayers.Brush(0xFF, 0xF4, 0xEB, 0xDD);
    private static readonly Brush HoneyDot = DrawingLayers.Brush(0xFF, 0xCD, 0xBB, 0x9C);
    private static readonly Brush HaloNear = DrawingLayers.Brush(0x50, 0xE7, 0xB7, 0x5F);
    private static readonly Brush HaloFar = DrawingLayers.Brush(0x24, 0xE7, 0xB7, 0x5F);

    private static readonly Pen RingPen = DrawingLayers.Pen(Ink, 1);
    private static readonly Pen OpenRingPen = DrawingLayers.Pen(InkFaint, 1, new DashStyle([2.5, 2], 0));
    private static readonly Pen GlyphPen = DrawingLayers.Pen(Ink, 1.5, cap: PenLineCap.Round);
    private static readonly Pen OpenGlyphPen = DrawingLayers.Pen(InkFaint, 1.5, cap: PenLineCap.Round);
    private static readonly Pen HoneyPen = DrawingLayers.Pen(DrawingLayers.Brush(0xFF, 0x9E, 0x2B, 0x1F), 1);
    private static readonly Pen LanternEdgeGold = DrawingLayers.Pen(Gold, 0.8, cap: PenLineCap.Round);
    private static readonly Pen LanternEdgeIce = DrawingLayers.Pen(Ice, 0.8, cap: PenLineCap.Round);
    private static readonly Pen LanternFootGold = DrawingLayers.Pen(Gold, 1);
    private static readonly Pen LanternFootIce = DrawingLayers.Pen(Ice, 1);
    private static readonly Pen CrownPen = DrawingLayers.Pen(InkGold, 0.6);
    private static readonly Pen TickGold = DrawingLayers.Pen(GoldDeep, 1);
    private static readonly Pen TickIce = DrawingLayers.Pen(Ice, 1);
    private static readonly Pen DetourPen = DrawingLayers.Pen(Ice, 1.5, new DashStyle([2, 2], 0), PenLineCap.Round);
    private static readonly Pen SledgePen = DrawingLayers.Pen(IceInk, 1.8, cap: PenLineCap.Round);

    private static readonly Brush GoldGlow = Glow(Color.FromRgb(0xE7, 0xB7, 0x5F), 0xA0);
    private static readonly Brush IceGlow = Glow(Color.FromRgb(0x4F, 0x92, 0xB5), 0xA0);
    private static readonly Brush RestGlow = Glow(Color.FromRgb(0xE0, 0x8A, 0x4A), 0xA0);

    // Laterne im Raster 10×14 (wie im Entwurf), Krone 12×8
    private static readonly Geometry LanternRoof = DrawingLayers.Geometry("M 1,4.2 L 5,1.5 L 9,4.2 Z");
    private static readonly Geometry LanternRing = DrawingLayers.Geometry("M 5,0.3 V 1.5");
    private static readonly Geometry LanternBase = DrawingLayers.Geometry("M 1,10 H 9 L 7.6,12 H 2.4 Z");
    private static readonly Geometry LanternFoot = DrawingLayers.Geometry("M 5,12 V 13.7");
    private static readonly Geometry Crown = DrawingLayers.Geometry("M 1,7.2 L 0.6,2 L 3.6,4.3 L 6,0.6 L 8.4,4.3 L 11.4,2 L 11,7.2 Z");

    private static Geometry?[]? s_glyphs;

    private readonly DrawingLayers _surface;
    private readonly MatrixTransform _trailTransform = new();
    private readonly TranslateTransform _tickTransform = new();
    private readonly TranslateTransform _markerTransform = new();

    public JourneyBand()
    {
        Focusable = false;
        IsTabStop = false;
        _surface = new DrawingLayers(6, BandHeight, RedrawAll);
        _surface[TrailLayer].Transform = _trailTransform;
        _surface[TickLayer].Transform = _tickTransform;
        _surface[MarkerLayer].Transform = _markerTransform;
        Content = _surface;
    }

    /// <summary>Aktuelle Station 1 bis 10.</summary>
    public int Station
    {
        get => (int)GetValue(StationProperty);
        set => SetValue(StationProperty, value);
    }

    /// <summary>Position der Laterne, 0 bis 1 (gemessene ÷ geplante Zeit).</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Wo jede Verlockung begann (Anteile 0 bis 1 der geplanten Zeit) – je Eintrag ein Stück Türkischer Honig.</summary>
    public IEnumerable<double>? HoneyMarks
    {
        get => (IEnumerable<double>?)GetValue(HoneyMarksProperty);
        set => SetValue(HoneyMarksProperty, value);
    }

    /// <summary>Ablenkung: eisblaue Laterne und gestrichelter Abweg zum Schlitten.</summary>
    public bool IsFrost
    {
        get => (bool)GetValue(IsFrostProperty);
        set => SetValue(IsFrostProperty, value);
    }

    /// <summary>Rast: Die Laterne glimmt warm, die Reise steht.</summary>
    public bool IsResting
    {
        get => (bool)GetValue(IsRestingProperty);
        set => SetValue(IsRestingProperty, value);
    }

    /// <summary>Krone über der Laterne (Ende im Frühling).</summary>
    public bool ShowCrown
    {
        get => (bool)GetValue(ShowCrownProperty);
        set => SetValue(ShowCrownProperty, value);
    }

    private double TrackWidth => Math.Max(0, _surface.ActualWidth - (2 * Inset));

    private static int CoerceStation(object baseValue) =>
        baseValue is int n ? Math.Clamp(n, 1, StationCount) : 1;

    private static double CoerceFraction(object baseValue) =>
        baseValue is double value && !double.IsNaN(value) ? Math.Clamp(value, 0, 1) : 0;

    private static void OnStationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyBand)d).DrawMedallions();

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyBand)d).ApplyProgress();

    private static void OnHoneyMarksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyBand)d).DrawHoney();

    private static void OnMarkerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var band = (JourneyBand)d;
        band.DrawTick();
        band.DrawMarker();
    }

    private void RedrawAll()
    {
        DrawTrack();
        DrawTrail();
        DrawTick();
        DrawMedallions();
        DrawHoney();
        DrawMarker();
        ApplyProgress();
    }

    /// <summary>Verschiebt Spur und Laterne – ohne neu zu zeichnen, und nur, wenn sich der Pixel ändert.</summary>
    private void ApplyProgress()
    {
        var x = Math.Round(Inset + (TrackWidth * Progress));
        if (x == _markerTransform.X && _trailTransform.Matrix.M11 == Math.Max(0, x - Inset))
        {
            return;
        }

        _trailTransform.Matrix = new Matrix(Math.Max(0, x - Inset), 0, 0, 1, Inset, 0);
        _tickTransform.X = x;
        _markerTransform.X = x;
    }

    /// <summary>Punktierte Linie: Punkte mit 2,5 px Durchmesser alle 7 px.</summary>
    private void DrawTrack()
    {
        using var dc = _surface[TrackLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        for (var x = Inset + 3.5; x <= Inset + width; x += 7)
        {
            dc.DrawEllipse(Ink, null, new Point(x, TrackY), 1.25, 1.25);
        }
    }

    /// <summary>Goldene Spur als Einheitsrechteck; die Breite setzt die Transformation.</summary>
    private void DrawTrail()
    {
        using var dc = _surface[TrailLayer].RenderOpen();
        dc.DrawRectangle(GoldDeep, null, new Rect(0, TrackY - 1, 1, 2));
    }

    /// <summary>Senkrechter Strich von der Laterne zur Linie (eisblau bei Ablenkung).</summary>
    private void DrawTick()
    {
        using var dc = _surface[TickLayer].RenderOpen();
        dc.DrawLine(IsFrost ? TickIce : TickGold, new Point(0.5, 30), new Point(0.5, TrackY - 1));
    }

    private void DrawMedallions()
    {
        using var dc = _surface[MedallionLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        var glyphs = StationGlyphs();
        var current = Station;
        for (var k = 1; k <= StationCount; k++)
        {
            var center = new Point(Math.Round(Inset + (width * (k - 1) / (StationCount - 1))), TrackY);
            var reached = k <= current;
            var isCurrent = k == current;
            var radius = isCurrent ? 16 : 14;

            if (isCurrent)
            {
                // Leuchten wie box-shadow 0 0 10px: zwei blasse Ringe statt eines Effekts
                dc.DrawEllipse(HaloFar, null, center, radius + 7, radius + 7);
                dc.DrawEllipse(HaloNear, null, center, radius + 3.5, radius + 3.5);
            }

            dc.DrawEllipse(reached ? Gold : Paper, reached ? RingPen : OpenRingPen, center, radius - 0.5, radius - 0.5);

            if (glyphs[k - 1] is { } glyph)
            {
                var size = isCurrent ? 22 : 20;
                var scale = size / 20d;
                dc.PushTransform(new MatrixTransform(scale, 0, 0, scale, center.X - (size / 2d), center.Y - (size / 2d)));
                dc.DrawGeometry(null, reached ? GlyphPen : OpenGlyphPen, glyph);
                dc.Pop();
            }
        }
    }

    /// <summary>Je Verlockung ein Stück Türkischer Honig über der Linie, dort wo sie begann.</summary>
    private void DrawHoney()
    {
        using var dc = _surface[HoneyLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0 || HoneyMarks is not { } marks)
        {
            return;
        }

        foreach (var mark in marks)
        {
            if (double.IsNaN(mark))
            {
                continue;
            }

            var left = Math.Round(Inset + (width * Math.Clamp(mark, 0, 1)) - 4.5);
            dc.DrawRoundedRectangle(HoneyFill, HoneyPen, new Rect(left + 1, 5, 7, 7), 1.2, 1.2);
            dc.DrawEllipse(HoneyDot, null, new Point(left + 3.3, 7.5), 0.75, 0.75);
            dc.DrawEllipse(HoneyDot, null, new Point(left + 5.8, 9.7), 0.75, 0.75);
        }
    }

    /// <summary>Laterne (um x = 0 gezeichnet), darüber ggf. die Krone, bei Ablenkung der Abweg zum Schlitten.</summary>
    private void DrawMarker()
    {
        using var dc = _surface[MarkerLayer].RenderOpen();
        var frost = IsFrost;

        if (frost)
        {
            dc.DrawLine(DetourPen, new Point(1, 22), new Point(16, 4));
            if (StationGlyphs()[1] is { } sledge)
            {
                dc.PushTransform(new MatrixTransform(0.7, 0, 0, 0.7, 15, -9));
                dc.DrawGeometry(null, SledgePen, sledge);
                dc.Pop();
            }
        }

        // Lichtschein hinter der Laterne
        dc.DrawEllipse(frost ? IceGlow : IsResting ? RestGlow : GoldGlow, null, new Point(0, 22.5), 11, 12);

        // Laterne 12×17 px ab y = 14 (Raster 10×14, Maßstab 1,2)
        var body = frost ? Ice : Gold;
        var edge = frost ? LanternEdgeIce : LanternEdgeGold;
        dc.PushTransform(new MatrixTransform(1.2, 0, 0, 1.2, -6, 14));
        dc.DrawGeometry(null, frost ? LanternFootIce : LanternFootGold, LanternRing);
        dc.DrawGeometry(body, edge, LanternRoof);
        dc.DrawRoundedRectangle(body, null, new Rect(1.6, 4.2, 6.8, 5.8), 0.6, 0.6);
        dc.DrawRectangle(frost ? IceWindow : WarmWindow, null, new Rect(3.1, 5.3, 3.8, 3.6));
        dc.DrawGeometry(body, edge, LanternBase);
        dc.DrawGeometry(null, frost ? LanternFootIce : LanternFootGold, LanternFoot);
        dc.Pop();

        if (ShowCrown)
        {
            dc.PushTransform(new TranslateTransform(-6, 5));
            dc.DrawGeometry(Gold, CrownPen, Crown);
            dc.Pop();
        }
    }

    /// <summary>Die zehn Stationszeichen aus dem Thema (Station1Geometry … Station10Geometry), einmal geladen.</summary>
    private static Geometry?[] StationGlyphs()
    {
        if (s_glyphs is not null)
        {
            return s_glyphs;
        }

        var glyphs = new Geometry?[StationCount];
        for (var i = 0; i < StationCount; i++)
        {
            glyphs[i] = DrawingLayers.ThemeGeometry("Station" + (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "Geometry");
        }

        // Nur merken, wenn das Thema schon geladen war (im Designer fehlt es)
        if (glyphs[0] is not null)
        {
            s_glyphs = glyphs;
        }

        return glyphs;
    }

    private static RadialGradientBrush Glow(Color color, byte alpha)
    {
        var brush = new RadialGradientBrush(Color.FromArgb(alpha, color.R, color.G, color.B), Color.FromArgb(0, color.R, color.G, color.B));
        brush.Freeze();
        return brush;
    }
}
