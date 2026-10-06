using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace Laternenwacht.App.Views;

/// <summary>
/// Ein kleines, nicht aktivierendes Fenster, das in einer Bahn über den Arbeitsbereich treibt.
/// Die Bewegung wird pro Bildschirmbild zeitbasiert berechnet (kein Ruckeln bei Last),
/// statt ein bildschirmgroßes transparentes Fenster zu animieren (das wäre teuer).
/// </summary>
public partial class MemeFloatWindow : Window
{
    private static readonly TimeSpan SinkDuration = TimeSpan.FromMilliseconds(700);
    private const double BobAmplitude = 22;
    private const double TiltDegrees = 5;

    private readonly TimeSpan _duration;
    private readonly Stopwatch _clock = new();
    private readonly bool _leftToRight = Random.Shared.Next(2) == 0;
    private readonly double _laneFraction = 0.2 + (Random.Shared.NextDouble() * 0.45);
    private readonly double _phase = Random.Shared.NextDouble() * Math.PI * 2;
    private TimeSpan? _sinkStartedAt;
    private Point _sinkFrom;

    public MemeFloatWindow(ImageSource image, string caption, TimeSpan duration)
    {
        InitializeComponent();
        ArgumentNullException.ThrowIfNull(image);
        Picture.ImageSource = image;

        // Rahmen an das Seitenverhältnis anpassen (Breite fest, Höhe 160–320).
        if (image.Width > 0 && image.Height > 0)
        {
            ImageFrame.Height = Math.Clamp(ImageFrame.Width * image.Height / image.Width, 160, 320);
        }
        CaptionText.Text = caption;
        _duration = duration;

        SourceInitialized += (_, _) => WindowStyles.MakeNonActivatingToolWindow(this);
        Loaded += (_, _) =>
        {
            _clock.Start();
            CompositionTarget.Rendering += OnFrame;
        };
        Closed += (_, _) => CompositionTarget.Rendering -= OnFrame;
        MouseLeftButtonUp += (_, _) => Sink();
    }

    /// <summary>Lässt das Meme nach unten wegsinken und schließt es dann.</summary>
    public void Sink()
    {
        if (_sinkStartedAt is null)
        {
            _sinkStartedAt = _clock.Elapsed;
            _sinkFrom = new Point(Left, Top);
        }
    }

    private void OnFrame(object? sender, EventArgs e)
    {
        var area = SystemParameters.WorkArea;
        var elapsed = _clock.Elapsed;

        if (_sinkStartedAt is { } sinkStart)
        {
            var s = Math.Clamp((elapsed - sinkStart) / SinkDuration, 0, 1);
            Top = _sinkFrom.Y + (s * s * 160);
            Opacity = 1 - s;
            Tilt.Angle += _leftToRight ? 1.5 : -1.5;
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
        Tilt.Angle = Math.Sin((t * Math.PI * 2 * 1.7) + _phase) * TiltDegrees;

        // Sanftes Auf- und Abtauchen an den Rändern.
        Opacity = Math.Min(1, Math.Min(t, 1 - t) / 0.06);
    }
}
