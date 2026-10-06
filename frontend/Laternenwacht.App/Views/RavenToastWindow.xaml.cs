using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Laternenwacht.App.Views;

/// <summary>
/// Eigene Push-Benachrichtigung. Bewusst kein Windows-Toast: Der funktioniert für nicht paketierte
/// Anwendungen nur mit Startmenü-Verknüpfung und registrierter App-ID, stiehlt je nach Einstellung
/// Aufmerksamkeit über das Info-Center und passt nicht zur Gestaltung.
/// Diese Botschaft aktiviert sich nie, verschwindet von selbst und pausiert, solange die Maus darüber ist.
/// </summary>
public partial class RavenToastWindow : Window
{
    private static readonly Duration FlyDuration = new(TimeSpan.FromMilliseconds(450));

    /// <summary>Der Restzeit-Balken ist schmal und langsam – 20 Bilder je Sekunde genügen.</summary>
    private const int CountdownFrameRate = 20;
    private readonly TimeSpan _lifetime;
    private AnimationClock? _lifetimeClock;
    private bool _closing;

    public RavenToastWindow(string header, string message, string source, string avatar, TimeSpan lifetime, bool positive)
    {
        InitializeComponent();
        HeaderText.Text = header.ToUpper(System.Globalization.CultureInfo.CurrentCulture);
        MessageText.Text = message;
        SourceText.Text = source;
        AvatarText.Text = avatar;
        _lifetime = lifetime;

        if (!positive)
        {
            // Mahnrufe gehören zum Frost: Eisblau statt Kerzenlicht.
            var frost = (System.Windows.Media.Brush)FindResource("FrostBrush");
            Avatar.Background = (System.Windows.Media.Brush)FindResource("FrostTintBrush");
            Avatar.BorderBrush = frost;
            AvatarGlow.Color = (System.Windows.Media.Color)FindResource("FrostColor");
            AvatarText.Foreground = frost;
            HeaderText.Foreground = frost;
            Lifetime.Background = frost;
        }

        SourceInitialized += (_, _) => WindowStyles.MakeNonActivatingToolWindow(this);
        Loaded += (_, _) => FlyIn();
        SizeChanged += (_, _) => PlaceBottomRight();
        MouseLeftButtonUp += (_, _) => FlyOut();
        MouseEnter += (_, _) => _lifetimeClock?.Controller.Pause();
        MouseLeave += (_, _) => _lifetimeClock?.Controller.Resume();
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                FlyOut();
            }
        };
    }

    /// <summary>Schließt die Botschaft sanft (z. B. wenn eine neue eintrifft).</summary>
    public void FlyOut()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        _lifetimeClock?.Controller.Stop();
        var fade = new DoubleAnimation(0, FlyDuration);
        fade.Completed += (_, _) => Close();
        Slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation(60, FlyDuration));
        Root.BeginAnimation(OpacityProperty, fade);
    }

    private void PlaceBottomRight()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - 8;
        Top = area.Bottom - ActualHeight - 8;
    }

    private void FlyIn()
    {
        PlaceBottomRight();
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        Slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, new DoubleAnimation(0, FlyDuration) { EasingFunction = ease });
        Root.BeginAnimation(OpacityProperty, new DoubleAnimation(1, FlyDuration));

        var countdown = new DoubleAnimation(1, 0, new Duration(_lifetime));
        Timeline.SetDesiredFrameRate(countdown, CountdownFrameRate);
        countdown.Completed += (_, _) => FlyOut();
        _lifetimeClock = countdown.CreateClock();
        LifetimeScale.ApplyAnimationClock(System.Windows.Media.ScaleTransform.ScaleXProperty, _lifetimeClock);
    }
}
