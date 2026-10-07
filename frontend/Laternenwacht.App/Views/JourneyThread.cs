using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Der Reisefaden der dunklen Fokusleiste: eine feine Linie mit zehn Punkten (die Stationen),
/// winzigen Honigstücken und einer kleinen Laterne bei <see cref="Progress"/>. Die Farbe kommt aus
/// <see cref="Accent"/> (Laternengold im Licht, Eisblau bei Ablenkung).
/// </summary>
/// <remarks>
/// Höhe 20 px, Breite vom Layout (im Entwurf 150 px). Drei Zeichenebenen (<see cref="DrawingLayers"/>): Linie und Punkte
/// werden nur bei Station, Farbe oder Größe neu gezeichnet, der Honig nur bei neuer Liste; die Laterne wird beim Fortschritt
/// lediglich verschoben (auf ganze Pixel gerundet).
/// </remarks>
public sealed class JourneyThread : UserControl
{
    /// <summary>Gesamthöhe des Fadens.</summary>
    public const double ThreadHeight = 20;

    private const double Inset = 4;
    private const double LineY = 14;
    private const int StationCount = 10;

    private const int ThreadLayer = 0;
    private const int HoneyLayer = 1;
    private const int LanternLayer = 2;

    public static readonly DependencyProperty StationProperty = DependencyProperty.Register(
        nameof(Station), typeof(int), typeof(JourneyThread), new PropertyMetadata(1, OnThreadChanged, (_, value) => CoerceStation(value)));

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(JourneyThread), new PropertyMetadata(0d, OnProgressChanged, (_, value) => CoerceFraction(value)));

    public static readonly DependencyProperty HoneyMarksProperty = DependencyProperty.Register(
        nameof(HoneyMarks), typeof(IEnumerable<double>), typeof(JourneyThread), new PropertyMetadata(null, OnHoneyMarksChanged));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(Brush), typeof(JourneyThread),
        new PropertyMetadata(DrawingLayers.Brush(0xFF, 0xE7, 0xB7, 0x5F), OnAccentChanged));

    private static readonly Brush OpenRing = DrawingLayers.Brush(0x66, 0xF1, 0xE6, 0xCC);
    private static readonly Pen OpenRingPen = DrawingLayers.Pen(OpenRing, 1);
    private static readonly Brush HoneyFill = DrawingLayers.Brush(0xFF, 0xF4, 0xEB, 0xDD);
    private static readonly Pen HoneyPen = DrawingLayers.Pen(DrawingLayers.Brush(0xFF, 0xC0, 0x50, 0x3F), 0.8);
    private static readonly Brush DefaultAccent = DrawingLayers.Brush(0xFF, 0xE7, 0xB7, 0x5F);
    private static readonly Brush DefaultWindow = DrawingLayers.Brush(0xFF, 0xFF, 0xF0, 0xC4);

    // Laterne im Raster 10×14, gezeichnet im Maßstab 0,7 (7×10 px)
    private static readonly Geometry LanternRoof = DrawingLayers.Geometry("M 1,4.2 L 5,1.5 L 9,4.2 Z");
    private static readonly Geometry LanternBase = DrawingLayers.Geometry("M 1,10 H 9 L 7.6,12 H 2.4 Z");

    private readonly DrawingLayers _surface;
    private readonly TranslateTransform _lanternTransform = new();

    private Brush _accent = DefaultAccent;
    private Brush _accentLine = DrawingLayers.Brush(0x59, 0xE7, 0xB7, 0x5F);
    private Brush _halo = DrawingLayers.Brush(0x4D, 0xE7, 0xB7, 0x5F);
    private Brush _window = DefaultWindow;

    public JourneyThread()
    {
        Focusable = false;
        IsTabStop = false;
        _surface = new DrawingLayers(3, ThreadHeight, RedrawAll);
        _surface[LanternLayer].Transform = _lanternTransform;
        Content = _surface;
    }

    /// <summary>Aktuelle Station 1 bis 10.</summary>
    public int Station
    {
        get => (int)GetValue(StationProperty);
        set => SetValue(StationProperty, value);
    }

    /// <summary>Position der Laterne, 0 bis 1.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Wo jede Verlockung begann (Anteile 0 bis 1 der geplanten Zeit).</summary>
    public IEnumerable<double>? HoneyMarks
    {
        get => (IEnumerable<double>?)GetValue(HoneyMarksProperty);
        set => SetValue(HoneyMarksProperty, value);
    }

    /// <summary>Farbe von Linie, Punkten und Laterne (Volltonpinsel empfohlen; daraus werden Schein und Fenster abgeleitet).</summary>
    public Brush Accent
    {
        get => (Brush)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    private double TrackWidth => Math.Max(0, _surface.ActualWidth - (2 * Inset));

    private static int CoerceStation(object baseValue) =>
        baseValue is int n ? Math.Clamp(n, 1, StationCount) : 1;

    private static double CoerceFraction(object baseValue) =>
        baseValue is double value && !double.IsNaN(value) ? Math.Clamp(value, 0, 1) : 0;

    private static void OnThreadChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyThread)d).DrawThread();

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyThread)d).ApplyProgress();

    private static void OnHoneyMarksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((JourneyThread)d).DrawHoney();

    private static void OnAccentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var thread = (JourneyThread)d;
        thread.DeriveColors(e.NewValue as Brush);
        thread.DrawThread();
        thread.DrawLantern();
    }

    /// <summary>Leitet Linie (35 %), Schein (30 %) und das helle Laternenfenster aus der Akzentfarbe ab – nur beim Farbwechsel.</summary>
    private void DeriveColors(Brush? accent)
    {
        _accent = accent ?? DefaultAccent;
        if (accent is SolidColorBrush { Color: var c })
        {
            _accentLine = DrawingLayers.Brush(0x59, c.R, c.G, c.B);
            _halo = DrawingLayers.Brush(0x4D, c.R, c.G, c.B);
            _window = DrawingLayers.Brush(0xFF, Lighten(c.R), Lighten(c.G), Lighten(c.B));
        }
        else
        {
            _accentLine = _accent;
            _halo = _accent;
            _window = DefaultWindow;
        }

        static byte Lighten(byte channel) => (byte)(channel + ((255 - channel) * 0.75));
    }

    private void RedrawAll()
    {
        DrawThread();
        DrawHoney();
        DrawLantern();
        ApplyProgress();
    }

    private void ApplyProgress()
    {
        // Nur bei einem neuen Pixel setzen (die Leiste meldet den Fortschritt jede Sekunde).
        var x = Math.Round(Inset + (TrackWidth * Progress));
        if (x != _lanternTransform.X)
        {
            _lanternTransform.X = x;
        }
    }

    /// <summary>Linie und zehn Punkte: erreicht = gefüllt, aktuell = größer mit Schein, offen = Ring.</summary>
    private void DrawThread()
    {
        using var dc = _surface[ThreadLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        dc.DrawRectangle(_accentLine, null, new Rect(Inset, LineY - 0.5, width, 1));
        var current = Station;
        for (var k = 1; k <= StationCount; k++)
        {
            var center = new Point(Math.Round(Inset + (width * (k - 1) / (StationCount - 1))), LineY);
            if (k == current)
            {
                dc.DrawEllipse(_halo, null, center, 6.5, 6.5);
                dc.DrawEllipse(_accent, null, center, 4, 4);
            }
            else if (k < current)
            {
                dc.DrawEllipse(_accent, null, center, 3, 3);
            }
            else
            {
                dc.DrawEllipse(null, OpenRingPen, center, 2.5, 2.5);
            }
        }
    }

    /// <summary>Honigstücke (5×5 px) über der Linie.</summary>
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

            var left = Math.Round(Inset + (width * Math.Clamp(mark, 0, 1)) - 2.5);
            dc.DrawRoundedRectangle(HoneyFill, HoneyPen, new Rect(left + 0.5, 3.5, 4, 4), 0.7, 0.7);
        }
    }

    /// <summary>Kleine Laterne (7×10 px) um x = 0, mit weichem Schein.</summary>
    private void DrawLantern()
    {
        using var dc = _surface[LanternLayer].RenderOpen();
        dc.DrawEllipse(_halo, null, new Point(0, 5), 5.5, 6.5);
        dc.PushTransform(new MatrixTransform(0.7, 0, 0, 0.7, -3.5, 0));
        dc.DrawGeometry(_accent, null, LanternRoof);
        dc.DrawRectangle(_accent, null, new Rect(1.6, 4.2, 6.8, 5.8));
        dc.DrawRectangle(_window, null, new Rect(3.1, 5.3, 3.8, 3.6));
        dc.DrawGeometry(_accent, null, LanternBase);
        dc.Pop();
    }
}
