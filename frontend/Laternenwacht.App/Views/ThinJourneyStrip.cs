using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Die ultradünne Fokusleiste: ein haarfeiner Streifen (6 px) mit dunkler Spur, einer Füllung bis zum Fortschritt,
/// feinen Kerben an den Stationen 2 bis 9 (bei (n − 1) / 9) und winzigen Honigstücken an den Stellen, an denen eine
/// Verlockung begann. Licht = Laternengold, Frost (<see cref="IsDistracted"/>) = Eisblau auf eisgetönter Spur,
/// Rast (<see cref="IsResting"/>) = Herdglut (Bernstein).
/// </summary>
/// <remarks>
/// Ressourcen: drei Zeichenebenen (<see cref="DrawingLayers"/>). Spur und Füllung werden nur bei Zustands- oder
/// Größenwechsel neu gezeichnet, Kerben und Honig nur bei neuer Honigliste oder Größe. Der sekündliche Fortschritt
/// ändert lediglich die Breite der Füllung über eine vorhandene Skalierung – und das nur, wenn ein neuer ganzer
/// Pixel erreicht ist. Keine Effekte, keine Animation.
/// </remarks>
public sealed class ThinJourneyStrip : UserControl
{
    /// <summary>Höhe des sichtbaren Streifens.</summary>
    public const double StripHeight = 6;

    private const int StationCount = 10;

    private const int TrackLayer = 0;
    private const int FillLayer = 1;
    private const int MarkLayer = 2;

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(ThinJourneyStrip),
        new PropertyMetadata(0d, OnProgressChanged, (_, value) => CoerceFraction(value)));

    public static readonly DependencyProperty HoneyMarksProperty = DependencyProperty.Register(
        nameof(HoneyMarks), typeof(IEnumerable<double>), typeof(ThinJourneyStrip), new PropertyMetadata(null, OnMarksChanged));

    public static readonly DependencyProperty IsDistractedProperty = DependencyProperty.Register(
        nameof(IsDistracted), typeof(bool), typeof(ThinJourneyStrip), new PropertyMetadata(false, OnStateChanged));

    public static readonly DependencyProperty IsRestingProperty = DependencyProperty.Register(
        nameof(IsResting), typeof(bool), typeof(ThinJourneyStrip), new PropertyMetadata(false, OnStateChanged));

    // Spur je Zustand: dunkle Nacht, bei Ablenkung deutlich heller und eisgetönt (Frost muss auch bei kaum gefüllter
    // Leiste sofort auffallen), warm in der Rast
    private static readonly Brush LightTrack = DrawingLayers.Brush(0xEB, 0x0B, 0x16, 0x13);
    private static readonly Brush FrostTrack = DrawingLayers.Brush(0xEB, 0x2E, 0x5C, 0x74);
    private static readonly Brush RestTrack = DrawingLayers.Brush(0xEB, 0x17, 0x0E, 0x09);

    // Füllung und feiner Saum an der Unterkante je Zustand
    private static readonly Brush LightFill = GoldGradient();
    private static readonly Brush FrostFill = DrawingLayers.Brush(0xFF, 0x9F, 0xD0, 0xEA);
    private static readonly Brush RestFill = DrawingLayers.Brush(0xFF, 0xE0, 0x8A, 0x4A);
    private static readonly Brush LightHem = DrawingLayers.Brush(0x73, 0xC9, 0xA0, 0x56);
    private static readonly Brush FrostHem = DrawingLayers.Brush(0xFF, 0x9F, 0xD0, 0xEA);
    private static readonly Brush RestHem = DrawingLayers.Brush(0x73, 0xE0, 0x8A, 0x4A);

    private static readonly Brush Notch = DrawingLayers.Brush(0x66, 0xF1, 0xE6, 0xCC);
    private static readonly Brush HoneyEdge = DrawingLayers.Brush(0xFF, 0xC0, 0x50, 0x3F);
    private static readonly Brush HoneyFill = DrawingLayers.Brush(0xFF, 0xF4, 0xEB, 0xDD);

    private readonly DrawingLayers _surface;
    private readonly ScaleTransform _fillScale = new(0, 1);
    private double _filledPixels = -1;

    public ThinJourneyStrip()
    {
        Focusable = false;
        IsTabStop = false;
        _surface = new DrawingLayers(3, StripHeight, RedrawAll);
        _surface[FillLayer].Transform = _fillScale;
        Content = _surface;
    }

    /// <summary>Fortschritt der Wacht, 0 bis 1.</summary>
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

    /// <summary>Frost: Füllung in Eisblau auf eisgetönter Spur.</summary>
    public bool IsDistracted
    {
        get => (bool)GetValue(IsDistractedProperty);
        set => SetValue(IsDistractedProperty, value);
    }

    /// <summary>Rast: Füllung in Herdglut (Bernstein).</summary>
    public bool IsResting
    {
        get => (bool)GetValue(IsRestingProperty);
        set => SetValue(IsRestingProperty, value);
    }

    private double TrackWidth => Math.Max(0, _surface.ActualWidth);

    private static double CoerceFraction(object baseValue) =>
        baseValue is double value && !double.IsNaN(value) ? Math.Clamp(value, 0, 1) : 0;

    private static void OnProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ThinJourneyStrip)d).ApplyProgress();

    private static void OnMarksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ThinJourneyStrip)d).DrawMarks();

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var strip = (ThinJourneyStrip)d;
        strip.DrawTrack();
        strip.DrawFill();
    }

    private static LinearGradientBrush GoldGradient()
    {
        var brush = new LinearGradientBrush(Color.FromRgb(0xF0, 0xC1, 0x6C), Color.FromRgb(0xC8, 0x91, 0x3A), 90);
        brush.Freeze();
        return brush;
    }

    private (Brush Track, Brush Fill, Brush Hem) Palette() =>
        IsResting ? (RestTrack, RestFill, RestHem)
        : IsDistracted ? (FrostTrack, FrostFill, FrostHem)
        : (LightTrack, LightFill, LightHem);

    private void RedrawAll()
    {
        DrawTrack();
        DrawFill();
        DrawMarks();
        _filledPixels = -1;
        ApplyProgress();
    }

    /// <summary>
    /// Spur über die ganze Breite mit feinem Saum in der Zustandsfarbe an der Unterkante: 1 px, im Frost 2 px und voll
    /// gedeckt (Eisblau), damit eine Ablenkung auch ganz am Anfang der Wacht unübersehbar ist.
    /// </summary>
    private void DrawTrack()
    {
        using var dc = _surface[TrackLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        var (track, _, hem) = Palette();
        var hemHeight = IsDistracted && !IsResting ? 2 : 1;
        dc.DrawRectangle(track, null, new Rect(0, 0, width, StripHeight));
        dc.DrawRectangle(hem, null, new Rect(0, StripHeight - hemHeight, width, hemHeight));
    }

    /// <summary>Füllung über die volle Breite; sichtbar wird davon nur der Anteil, den die Skalierung freigibt.</summary>
    private void DrawFill()
    {
        using var dc = _surface[FillLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        var (_, fill, _) = Palette();
        dc.DrawRectangle(fill, null, new Rect(0, 0, width, StripHeight));
    }

    /// <summary>Kerben an den Stationen 2–9 (Anfang und Ende sind die Kanten) und Honigstücke (3 × 6 px).</summary>
    private void DrawMarks()
    {
        using var dc = _surface[MarkLayer].RenderOpen();
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        for (var n = 2; n < StationCount; n++)
        {
            var x = Math.Round(width * (n - 1) / (StationCount - 1));
            dc.DrawRectangle(Notch, null, new Rect(x, 1, 1, StripHeight - 2));
        }

        if (HoneyMarks is not { } marks)
        {
            return;
        }

        foreach (var mark in marks)
        {
            if (double.IsNaN(mark))
            {
                continue;
            }

            var x = Math.Clamp(Math.Round(width * Math.Clamp(mark, 0, 1)), 1, Math.Max(1, width - 2));
            dc.DrawRectangle(HoneyEdge, null, new Rect(x - 1, 0, 3, StripHeight));
            dc.DrawRectangle(HoneyFill, null, new Rect(x, 1, 1, StripHeight - 2));
        }
    }

    private void ApplyProgress()
    {
        var width = TrackWidth;
        if (width <= 0)
        {
            return;
        }

        // Nur bei einem neuen ganzen Pixel umstellen (der Fortschritt kommt jede Sekunde).
        var pixels = Math.Round(width * Progress);
        if (pixels == _filledPixels)
        {
            return;
        }

        _filledPixels = pixels;
        _fillScale.ScaleX = pixels / width;
    }
}
