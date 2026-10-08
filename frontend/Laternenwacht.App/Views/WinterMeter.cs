using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Views;

/// <summary>
/// "Der Winter der Königin": wie viel Frost (Ablenkung) die Wacht schon gesammelt hat, gemessen an der
/// geplanten Dauer. Die Skala reicht bis 40 % der geplanten Zeit und ist in drei Jahreszeiten geteilt:
/// Frühling bis 10 % (ein Viertel der Skala), Tauwetter bis 25 % (bis 62,5 %), danach Winter.
/// Kerben markieren die Grenzen, darunter stehen die Namen. Die Füllung wächst nur – Frost schmilzt nicht.
/// </summary>
/// <remarks>
/// Höhe 36 px (Balken 10 px, Beschriftung darunter). Skala, Kerben und Beschriftung werden nur bei Größenänderung
/// gezeichnet; die eisblaue Füllung nur, wenn sie sich um mindestens einen halben Pixel ändert.
/// Die Grenzen stammen aus <see cref="RealmMoods"/> und stimmen so immer mit der Jahreszeit am Ende überein.
/// </remarks>
public sealed class WinterMeter : UserControl
{
    /// <summary>Gesamthöhe des Messers.</summary>
    public const double MeterHeight = 36;

    /// <summary>Frostanteil der geplanten Zeit, bei dem die Skala voll ist.</summary>
    public const double ScaleMaximum = 0.4;

    private const double BarTop = 2;
    private const double BarHeight = 10;
    private const double Radius = BarHeight / 2;

    private const int ScaleLayer = 0;
    private const int FillLayer = 1;
    private const int MarksLayer = 2;

    public static readonly DependencyProperty FrostSecondsProperty = DependencyProperty.Register(
        nameof(FrostSeconds), typeof(double), typeof(WinterMeter), new PropertyMetadata(0d, OnValueChanged));

    public static readonly DependencyProperty PlannedSecondsProperty = DependencyProperty.Register(
        nameof(PlannedSeconds), typeof(double), typeof(WinterMeter), new PropertyMetadata(0d, OnValueChanged));

    private static readonly Brush SpringZone = DrawingLayers.Brush(0x59, 0xB9, 0xD5, 0x8E);
    private static readonly Brush ThawZone = DrawingLayers.Brush(0x59, 0x9F, 0xD0, 0xEA);
    private static readonly Brush WinterZone = DrawingLayers.Brush(0x59, 0x8E, 0x9D, 0xAA);
    private static readonly Pen Outline = DrawingLayers.Pen(DrawingLayers.Brush(0x40, 0x6B, 0x54, 0x34), 1);
    private static readonly Brush Fill = DrawingLayers.Brush(0xFF, 0x4F, 0x92, 0xB5);
    private static readonly Brush Notch = DrawingLayers.Brush(0xFF, 0x2A, 0x1E, 0x14);
    private static readonly Brush Label = DrawingLayers.Brush(0xFF, 0x6B, 0x54, 0x34);

    private readonly DrawingLayers _surface;
    private double _drawnFill = -1;

    public WinterMeter()
    {
        Focusable = false;
        IsTabStop = false;
        _surface = new DrawingLayers(3, MeterHeight, RedrawAll);
        Content = _surface;
    }

    /// <summary>Bisher gesammelter Frost in Sekunden.</summary>
    public double FrostSeconds
    {
        get => (double)GetValue(FrostSecondsProperty);
        set => SetValue(FrostSecondsProperty, value);
    }

    /// <summary>Geplante Dauer der Wacht in Sekunden.</summary>
    public double PlannedSeconds
    {
        get => (double)GetValue(PlannedSecondsProperty);
        set => SetValue(PlannedSecondsProperty, value);
    }

    /// <summary>Anteil der Skala bei der Grenze Frühling/Tauwetter (0,25).</summary>
    private static double ThawMark => RealmMoods.ThawThreshold / ScaleMaximum;

    /// <summary>Anteil der Skala bei der Grenze Tauwetter/Winter (0,625).</summary>
    private static double WinterMark => RealmMoods.WinterThreshold / ScaleMaximum;

    /// <summary>Füllung 0 bis 1 = Frost ÷ geplante Zeit ÷ 0,4.</summary>
    private double FillFraction
    {
        get
        {
            var planned = PlannedSeconds;
            var frost = FrostSeconds;
            if (!(planned > 0) || !(frost > 0))
            {
                return 0;
            }

            return Math.Clamp(frost / planned / ScaleMaximum, 0, 1);
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((WinterMeter)d).DrawFill(force: false);

    private void RedrawAll()
    {
        DrawScale();
        DrawFill(force: true);
        DrawMarks();
    }

    private void DrawScale()
    {
        using var dc = _surface[ScaleLayer].RenderOpen();
        var width = _surface.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        var bar = new Rect(0, BarTop, width, BarHeight);
        var clip = new RectangleGeometry(bar, Radius, Radius);
        clip.Freeze();
        dc.PushClip(clip);
        dc.DrawRectangle(SpringZone, null, new Rect(0, BarTop, width * ThawMark, BarHeight));
        dc.DrawRectangle(ThawZone, null, new Rect(width * ThawMark, BarTop, width * (WinterMark - ThawMark), BarHeight));
        dc.DrawRectangle(WinterZone, null, new Rect(width * WinterMark, BarTop, width * (1 - WinterMark), BarHeight));
        dc.Pop();
        dc.DrawRoundedRectangle(null, Outline, new Rect(0.5, BarTop + 0.5, width - 1, BarHeight - 1), Radius - 0.5, Radius - 0.5);
    }

    /// <summary>Eisblaue Füllung; nur neu, wenn sie sich sichtbar ändert.</summary>
    private void DrawFill(bool force)
    {
        var width = _surface.ActualWidth;
        var fill = Math.Round(width * FillFraction * 2) / 2;
        if (!force && Math.Abs(fill - _drawnFill) < 0.5)
        {
            return;
        }

        _drawnFill = fill;
        using var dc = _surface[FillLayer].RenderOpen();
        if (fill <= 0)
        {
            return;
        }

        var radius = Math.Min(Radius, fill / 2);
        dc.DrawRoundedRectangle(Fill, null, new Rect(0, BarTop, fill, BarHeight), radius, radius);
    }

    /// <summary>Kerben an den Jahreszeitgrenzen und die Namen der drei Zonen.</summary>
    private void DrawMarks()
    {
        using var dc = _surface[MarksLayer].RenderOpen();
        var width = _surface.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        dc.DrawRectangle(Notch, null, new Rect(Math.Round(width * ThawMark) - 1, BarTop - 2, 2, BarHeight + 4));
        dc.DrawRectangle(Notch, null, new Rect(Math.Round(width * WinterMark) - 1, BarTop - 2, 2, BarHeight + 4));

        var typeface = new Typeface(
            TryFindResource("CinzelFont") as FontFamily ?? new FontFamily("Georgia"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        DrawLabel(dc, "Frühling", width * ThawMark / 2, typeface, pixelsPerDip);
        DrawLabel(dc, "· Tauwetter ·", width * (ThawMark + WinterMark) / 2, typeface, pixelsPerDip);
        DrawLabel(dc, "Winter", width * (WinterMark + 1) / 2, typeface, pixelsPerDip);
    }

    private static void DrawLabel(DrawingContext dc, string text, double centerX, Typeface typeface, double pixelsPerDip)
    {
        var formatted = new FormattedText(text, CultureInfo.GetCultureInfo("de-DE"), FlowDirection.LeftToRight, typeface, 11, Label, pixelsPerDip);
        dc.DrawText(formatted, new Point(Math.Round(centerX - (formatted.Width / 2)), BarTop + BarHeight + 6));
    }
}
