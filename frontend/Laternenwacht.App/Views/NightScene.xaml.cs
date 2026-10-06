using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Laternenwacht.App.Views;

/// <summary>
/// Illustrierte Nachtwald-Szene mit Laterne, Lichtring (Fortschritt) und fallendem Schnee.
/// </summary>
/// <remarks>
/// Ressourcenschonend aufgebaut:
/// <list type="bullet">
/// <item>Der Schnee besteht aus nur drei Tiefenebenen (je ein Element mit allen Flocken als eine Geometrie)
/// mit je einer Fall- und einer Pendelanimation – statt einem eigenen Element mit zwei Animationen pro Flocke.
/// Jede Ebene enthält ihre Flocken doppelt, um eine Szenenhöhe versetzt; so schließt die Endlosschleife nahtlos.</item>
/// <item>Bildraten sind gedrosselt (Schnee 24, Atmen 15 Bilder je Sekunde).</item>
/// <item>Alles ruht, solange das Fenster nicht im Vordergrund ist (<see cref="AmbientMotion"/>).</item>
/// </list>
/// </remarks>
public partial class NightScene : UserControl
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(NightScene), new PropertyMetadata(0d));

    public static readonly DependencyProperty IsFrostProperty = DependencyProperty.Register(
        nameof(IsFrost), typeof(bool), typeof(NightScene), new PropertyMetadata(false, OnIsFrostChanged));

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush), typeof(Brush), typeof(NightScene), new PropertyMetadata(Brushes.Goldenrod));

    private const double SceneWidth = 560;
    private const double SceneHeight = 520;
    private const int SnowFrameRate = 24;

    /// <summary>Tiefenebenen des Schnees: fern = klein, blass, langsam; nah = groß, hell, schneller.</summary>
    private static readonly SnowDepth[] SnowDepths =
    [
        new(Count: 12, MinSize: 1.4, MaxSize: 2.2, Opacity: 0.28, FallSeconds: 30, Sway: 4, SwaySeconds: 4.6),
        new(Count: 9, MinSize: 2.0, MaxSize: 3.0, Opacity: 0.42, FallSeconds: 20, Sway: 7, SwaySeconds: 3.7),
        new(Count: 5, MinSize: 2.8, MaxSize: 4.2, Opacity: 0.58, FallSeconds: 13, Sway: 10, SwaySeconds: 2.9),
    ];

    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(900));
    private static readonly Brush WarmGlass = Frozen(new SolidColorBrush(Color.FromRgb(0xFF, 0xE3, 0xB3)));
    private static readonly Brush ColdGlass = Frozen(new SolidColorBrush(Color.FromRgb(0xCF, 0xEA, 0xF5)));

    private readonly Storyboard _breathe;
    private readonly Storyboard _snow;
    private bool _motionStarted;

    public NightScene()
    {
        InitializeComponent();
        _breathe = (Storyboard)Resources["Breathe"];
        _snow = CreateSnow();
        _ = new AmbientMotion(this, SetMotion);
        SizeChanged += (_, _) => Clip = new RectangleGeometry(new Rect(RenderSize), 24, 24);
    }

    /// <summary>Fortschritt der Wacht (0–1), dargestellt als Lichtring um die Laterne.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>Ablenkung: das Licht wird kalt.</summary>
    public bool IsFrost
    {
        get => (bool)GetValue(IsFrostProperty);
        set => SetValue(IsFrostProperty, value);
    }

    public Brush RingBrush
    {
        get => (Brush)GetValue(RingBrushProperty);
        set => SetValue(RingBrushProperty, value);
    }

    private static void OnIsFrostChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var scene = (NightScene)d;
        var frost = (bool)e.NewValue;
        Fade(scene.ColdGlow, frost ? 1 : 0);
        Fade(scene.ColdGround, frost ? 1 : 0);
        Fade(scene.WarmGround, frost ? 0 : 1);
        scene.WarmGlow.Visibility = frost ? Visibility.Hidden : Visibility.Visible;
        scene.Glass.Fill = frost ? ColdGlass : WarmGlass;
    }

    private static void Fade(UIElement element, double to) =>
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(to, FadeDuration));

    /// <summary>Startet, pausiert oder setzt die Bewegung fort – angehaltene Uhren kosten keine Rechenzeit.</summary>
    private void SetMotion(bool run)
    {
        if (run && !_motionStarted)
        {
            _breathe.Begin(this, isControllable: true);
            _snow.Begin(this, isControllable: true);
            _motionStarted = true;
        }
        else if (run)
        {
            _breathe.Resume(this);
            _snow.Resume(this);
        }
        else if (_motionStarted)
        {
            _breathe.Pause(this);
            _snow.Pause(this);
        }
    }

    /// <summary>Erzeugt die drei Schneeebenen und ihre Animationen (noch nicht gestartet).</summary>
    private Storyboard CreateSnow()
    {
        var random = new Random(42);
        var storyboard = new Storyboard();
        Timeline.SetDesiredFrameRate(storyboard, SnowFrameRate);

        foreach (var depth in SnowDepths)
        {
            var flakes = new GeometryGroup { FillRule = FillRule.Nonzero };
            for (var i = 0; i < depth.Count; i++)
            {
                var radius = (depth.MinSize + (random.NextDouble() * (depth.MaxSize - depth.MinSize))) / 2;
                var x = random.NextDouble() * SceneWidth;
                var y = random.NextDouble() * SceneHeight;
                flakes.Children.Add(new EllipseGeometry(new Point(x, y), radius, radius));
                flakes.Children.Add(new EllipseGeometry(new Point(x, y - SceneHeight), radius, radius));
            }

            flakes.Freeze();
            var layer = new System.Windows.Shapes.Path
            {
                Data = flakes,
                Fill = Brushes.White,
                Opacity = depth.Opacity,
                RenderTransform = new TranslateTransform(),
            };
            SnowLayer.Children.Add(layer);

            var fall = new DoubleAnimation(0, SceneHeight, TimeSpan.FromSeconds(depth.FallSeconds))
            {
                RepeatBehavior = RepeatBehavior.Forever,
            };
            var sway = new DoubleAnimation(-depth.Sway, depth.Sway, TimeSpan.FromSeconds(depth.SwaySeconds))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            AddAnimation(storyboard, fall, layer, "(UIElement.RenderTransform).(TranslateTransform.Y)");
            AddAnimation(storyboard, sway, layer, "(UIElement.RenderTransform).(TranslateTransform.X)");
        }

        return storyboard;
    }

    private static void AddAnimation(Storyboard storyboard, AnimationTimeline animation, DependencyObject target, string path)
    {
        Storyboard.SetTarget(animation, target);
        Storyboard.SetTargetProperty(animation, new PropertyPath(path));
        storyboard.Children.Add(animation);
    }

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }

    private sealed record SnowDepth(int Count, double MinSize, double MaxSize, double Opacity, double FallSeconds, double Sway, double SwaySeconds);
}
