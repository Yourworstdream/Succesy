using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Laternenwacht.App.Views;

/// <summary>
/// Illustrierte Nachtwald-Szene mit Laterne, Lichtring (Fortschritt) und fallendem Schnee.
/// Animationen entfallen, wenn Windows "Animationen anzeigen" ausgeschaltet ist.
/// </summary>
public partial class NightScene : UserControl
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(NightScene), new PropertyMetadata(0d));

    public static readonly DependencyProperty IsFrostProperty = DependencyProperty.Register(
        nameof(IsFrost), typeof(bool), typeof(NightScene), new PropertyMetadata(false, OnIsFrostChanged));

    public static readonly DependencyProperty RingBrushProperty = DependencyProperty.Register(
        nameof(RingBrush), typeof(Brush), typeof(NightScene), new PropertyMetadata(Brushes.Goldenrod));

    private const int FlakeCount = 26;
    private static readonly Duration FadeDuration = new(TimeSpan.FromMilliseconds(900));

    private readonly Storyboard _breathe;

    public NightScene()
    {
        InitializeComponent();
        _breathe = (Storyboard)Resources["Breathe"];
        CreateSnow();
        Loaded += (_, _) =>
        {
            if (SystemParameters.ClientAreaAnimation)
            {
                _breathe.Begin(this, isControllable: true);
            }
        };
        Unloaded += (_, _) => _breathe.Stop(this);
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
        scene.Glass.Fill = new SolidColorBrush(frost ? Color.FromRgb(0xCF, 0xEA, 0xF5) : Color.FromRgb(0xFF, 0xE3, 0xB3));
        if (scene.Ring.Effect is System.Windows.Media.Effects.DropShadowEffect glow)
        {
            glow.Color = frost ? Color.FromRgb(0x9F, 0xD3, 0xEA) : Color.FromRgb(0xE2, 0xC4, 0x8D);
        }
    }

    private static void Fade(UIElement element, double to) =>
        element.BeginAnimation(OpacityProperty, new DoubleAnimation(to, FadeDuration));

    /// <summary>Erzeugt sanft fallende Schneeflocken mit unterschiedlicher Größe, Tiefe und Geschwindigkeit.</summary>
    private void CreateSnow()
    {
        var random = new Random(42);
        var animate = SystemParameters.ClientAreaAnimation;

        for (var i = 0; i < FlakeCount; i++)
        {
            var size = 1.6 + (random.NextDouble() * 2.6);
            var flake = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = Brushes.White,
                Opacity = 0.25 + (random.NextDouble() * 0.4),
            };
            var x = random.NextDouble() * 560;
            var y = random.NextDouble() * 520;
            Canvas.SetLeft(flake, x);
            Canvas.SetTop(flake, 0);

            var move = new TranslateTransform(0, y);
            flake.RenderTransform = move;
            SnowLayer.Children.Add(flake);

            if (!animate)
            {
                continue;
            }

            var seconds = 10 + (random.NextDouble() * 9);
            var fall = new DoubleAnimation(-10, 530, TimeSpan.FromSeconds(seconds))
            {
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromSeconds(-seconds * (y / 520)),
            };
            var sway = new DoubleAnimation(-6 - (random.NextDouble() * 6), 6 + (random.NextDouble() * 6), TimeSpan.FromSeconds(2.5 + (random.NextDouble() * 2)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            move.BeginAnimation(TranslateTransform.YProperty, fall);
            move.BeginAnimation(TranslateTransform.XProperty, sway);
        }
    }
}
