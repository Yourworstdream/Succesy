using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Views;

/// <summary>Seite „Chronik“ (Kapitel II). Reine Darstellung, die Logik liegt im <see cref="ViewModels.ChronicleViewModel"/>.</summary>
/// <remarks>
/// Der Code hier ordnet nur die Flächen nach der verfügbaren Breite an – und auch das nur, wenn eine Schwelle
/// überschritten wird, nicht bei jeder Größenänderung.
/// </remarks>
public partial class ChroniclePage : UserControl
{
    /// <summary>Mindestbreite der Tabelle (Spalten 210 + 200 + 76 + 64 + 118 + 96 und die Bildlaufleiste); schmaler scrollt sie waagerecht.</summary>
    public const double LedgerMinWidth = 780;

    /// <summary>Ab dieser Seitenbreite steht „Die Bilder der Chronik“ rechts neben dem Verzeichnis.</summary>
    private const double SideBySideWidth = 1130;

    /// <summary>Unter dieser Seitenbreite stehen die Kennzahlen unter der Siegelkarte.</summary>
    private const double StackedTopWidth = 820;

    private bool? _sideBySide;
    private bool? _stackedTop;

    public ChroniclePage()
    {
        InitializeComponent();
        SizeChanged += (_, e) => ApplyLayout(e.NewSize.Width);
    }

    private void ApplyLayout(double width)
    {
        var sideBySide = width >= SideBySideWidth;
        if (_sideBySide != sideBySide)
        {
            _sideBySide = sideBySide;
            ArrangePictures(sideBySide);
        }

        var stackedTop = width < StackedTopWidth;
        if (_stackedTop != stackedTop)
        {
            _stackedTop = stackedTop;
            ArrangeTop(stackedTop);
        }
    }

    /// <summary>Seitenleiste rechts (breit) oder als flacher Streifen unter dem Verzeichnis (schmal).</summary>
    private void ArrangePictures(bool sideBySide)
    {
        if (sideBySide)
        {
            Grid.SetRow(PicturesPanel, 0);
            Grid.SetColumn(PicturesPanel, 1);
            Grid.SetColumnSpan(PicturesPanel, 1);
            PicturesPanel.Width = 260;
            PicturesPanel.Margin = new Thickness(24, 0, 0, 0);
            PictureRows.Columns = 1;
            QuoteColumn.Width = new GridLength(0);
            Grid.SetRow(QuotePart, 2);
            Grid.SetRowSpan(QuotePart, 1);
            Grid.SetColumn(QuotePart, 0);
            QuotePart.Margin = new Thickness(0, 10, 0, 0);
            QuoteDivider.Visibility = Visibility.Visible;
        }
        else
        {
            Grid.SetRow(PicturesPanel, 1);
            Grid.SetColumn(PicturesPanel, 0);
            Grid.SetColumnSpan(PicturesPanel, 2);
            PicturesPanel.Width = double.NaN;
            PicturesPanel.Margin = new Thickness(0, 20, 0, 0);
            PictureRows.Columns = 3;
            QuoteColumn.Width = new GridLength(0.8, GridUnitType.Star);
            Grid.SetRow(QuotePart, 0);
            Grid.SetRowSpan(QuotePart, 2);
            Grid.SetColumn(QuotePart, 1);
            QuotePart.Margin = new Thickness(32, 0, 0, 0);
            QuoteDivider.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>Siegelkarte und Kennzahlen nebeneinander (breit) oder untereinander (schmal).</summary>
    private void ArrangeTop(bool stacked)
    {
        if (stacked)
        {
            TopGapColumn.Width = new GridLength(0);
            MetricsColumn.Width = new GridLength(0);
            Grid.SetColumnSpan(SealPanel, 3);
            Grid.SetRow(MetricsPanel, 1);
            Grid.SetColumn(MetricsPanel, 0);
            Grid.SetColumnSpan(MetricsPanel, 3);
            MetricsPanel.Margin = new Thickness(0, 16, 0, 0);
        }
        else
        {
            TopGapColumn.Width = new GridLength(24);
            MetricsColumn.Width = new GridLength(2, GridUnitType.Star);
            Grid.SetColumnSpan(SealPanel, 1);
            Grid.SetRow(MetricsPanel, 0);
            Grid.SetColumn(MetricsPanel, 2);
            Grid.SetColumnSpan(MetricsPanel, 1);
            MetricsPanel.Margin = new Thickness(0);
        }
    }
}

/// <summary>
/// Kleines Wegband einer Chronikzeile (90 × 10): feine Linie mit zehn Punkten, gold bis zur erreichten Station,
/// danach hohl – wie das große Wegband der Wacht, nur ohne Honig und Laterne.
/// </summary>
/// <remarks>
/// Zeichnet sich selbst mit geteilten, eingefrorenen Pinseln und Stiften – keine Unterelemente je Punkt, damit lange
/// Chroniken leicht bleiben. Neu gezeichnet wird nur, wenn sich <see cref="Station"/> ändert (z. B. beim Wiederverwenden
/// einer Zeile im virtualisierten Verzeichnis).
/// </remarks>
public sealed class ChronicleJourneyMark : FrameworkElement
{
    private const double BandWidth = 90;
    private const double BandHeight = 10;
    private const double Inset = 3;
    private const double DotRadius = 2.5;

    public static readonly DependencyProperty StationProperty = DependencyProperty.Register(
        nameof(Station), typeof(int), typeof(ChronicleJourneyMark),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly Brush LineBrush = Frozen(Color.FromArgb(0x73, 0x6B, 0x54, 0x34));
    private static readonly Brush ReachedBrush = Frozen(Color.FromRgb(0xC8, 0x91, 0x3A));
    private static readonly Pen ReachedPen = FrozenPen(Color.FromRgb(0x8A, 0x5A, 0x12));
    private static readonly Pen OpenPen = FrozenPen(Color.FromArgb(0x80, 0x6B, 0x54, 0x34));

    /// <summary>Erreichte Station (0 = keine, 1–10).</summary>
    public int Station
    {
        get => (int)GetValue(StationProperty);
        set => SetValue(StationProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => new(BandWidth, BandHeight);

    protected override void OnRender(DrawingContext drawingContext)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);

        const double y = BandHeight / 2;
        const double span = BandWidth - (2 * Inset);
        drawingContext.DrawRectangle(LineBrush, null, new Rect(Inset, y - 0.5, span, 1));

        var reached = Math.Clamp(Station, 0, Journey.StationCount);
        for (var i = 0; i < Journey.StationCount; i++)
        {
            var center = new Point(Inset + (span * i / (Journey.StationCount - 1)), y);
            if (i < reached)
            {
                drawingContext.DrawEllipse(ReachedBrush, ReachedPen, center, DotRadius, DotRadius);
            }
            else
            {
                drawingContext.DrawEllipse(null, OpenPen, center, DotRadius, DotRadius);
            }
        }
    }

    private static SolidColorBrush Frozen(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(Color color)
    {
        var pen = new Pen(Frozen(color), 1);
        pen.Freeze();
        return pen;
    }
}
