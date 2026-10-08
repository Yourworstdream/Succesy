using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Laternenwacht.App.Views;

/// <summary>
/// Treibgut: die Schachtel der Königin mit einem Meme darin – ein kleines, nicht aktivierendes Fenster,
/// das in einer Bahn über den Arbeitsbereich treibt
/// (statt ein bildschirmgroßes transparentes Fenster zu animieren – das wäre teuer).
/// </summary>
/// <remarks>
/// Die Bewegung wird zeitbasiert berechnet (kein Ruckeln bei Last) und von einem eigenen 30-Hz-Takt angestoßen.
/// Bewusst nicht <see cref="CompositionTarget.Rendering"/>: Solange sich jemand dort anmeldet, zeichnet WPF
/// ununterbrochen mit voller Bildwiederholrate. Die Neigung wird auf Viertelgrad gerundet, damit das
/// transparente Fenster nur neu gezeichnet wird, wenn sich sichtbar etwas ändert – das reine Verschieben kostet fast nichts.
/// </remarks>
public partial class MemeFloatWindow : Window
{
    private static readonly TimeSpan SinkDuration = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan FrameInterval = TimeSpan.FromMilliseconds(1000.0 / 30);
    private const double BobAmplitude = 22;
    private const double TiltDegrees = 5;

    /// <summary>Breite des Bildfeldes in der Schachtel: 300 − 2 × 14 (Deckelrand) − 2 × 5 (Passepartout samt Goldkante).</summary>
    private const double PictureWidth = 262;

    private readonly TimeSpan _duration;
    private readonly Stopwatch _clock = new();
    private readonly DispatcherTimer _frames;
    private readonly bool _leftToRight = Random.Shared.Next(2) == 0;
    private readonly double _laneFraction = 0.2 + (Random.Shared.NextDouble() * 0.45);
    private readonly double _phase = Random.Shared.NextDouble() * Math.PI * 2;
    private TimeSpan? _sinkStartedAt;
    private Point _sinkFrom;
    private double _sinkTilt;

    public MemeFloatWindow(ImageSource image, string caption, TimeSpan duration)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(image);
        Picture.ImageSource = image;

        // Bildfeld an das Seitenverhältnis anpassen (Breite durch die Schachtel fest, Höhe 160–320).
        if (image.Width > 0 && image.Height > 0)
        {
            ImageFrame.Height = Math.Clamp(PictureWidth * image.Height / image.Width, 160, 320);
        }
        CaptionText.Text = caption;
        _duration = duration;

        _frames = new DispatcherTimer(DispatcherPriority.Render) { Interval = FrameInterval };
        _frames.Tick += (_, _) => OnFrame();

        SourceInitialized += (_, _) => WindowStyles.MakeNonActivatingToolWindow(this);
        Loaded += (_, _) =>
        {
            _clock.Start();
            OnFrame();
            _frames.Start();
        };
        Closed += (_, _) => _frames.Stop();
        MouseLeftButtonUp += (_, _) => Sink();
    }

    /// <summary>Lässt die Schachtel nach unten in den Schnee sinken und schließt das Fenster dann.</summary>
    public void Sink()
    {
        if (_sinkStartedAt is null)
        {
            _sinkStartedAt = _clock.Elapsed;
            _sinkFrom = new Point(Left, Top);
            _sinkTilt = Tilt.Angle;
        }
    }

    private void OnFrame()
    {
        var area = SystemParameters.WorkArea;
        var elapsed = _clock.Elapsed;

        if (_sinkStartedAt is { } sinkStart)
        {
            var s = Math.Clamp((elapsed - sinkStart) / SinkDuration, 0, 1);
            Top = _sinkFrom.Y + (s * s * 160);
            Opacity = 1 - s;
            SetTilt(_sinkTilt + ((_leftToRight ? 60 : -60) * s));
            if (s >= 1)
            {
                Close();
            }

            return;
        }

        var t = elapsed / _duration;
        if (t >= 1)
        {
            Close();
            return;
        }

        // Bahn: vollständig von außerhalb des einen Randes bis außerhalb des anderen.
        var startX = area.Left - ActualWidth;
        var endX = area.Right;
        var progress = _leftToRight ? t : 1 - t;
        var wave = Math.Sin((t * Math.PI * 2 * 2.5) + _phase);

        Left = startX + ((endX - startX) * progress);
        Top = area.Top + ((area.Height - ActualHeight) * _laneFraction) + (wave * BobAmplitude);
        SetTilt(Math.Sin((t * Math.PI * 2 * 1.7) + _phase) * TiltDegrees);

        // Sanftes Auf- und Abtauchen an den Rändern.
        Opacity = Math.Min(1, Math.Min(t, 1 - t) / 0.06);
    }

    /// <summary>Neigung auf Viertelgrad gerundet: Gleiche Werte lösen kein Neuzeichnen aus.</summary>
    private void SetTilt(double degrees) => Tilt.Angle = Math.Round(degrees * 4) / 4;
}
