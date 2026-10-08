using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Laternenwacht.App.Views;

/// <summary>Wer den Brief des Rabenboten schickt – bestimmt Medaillon, Siegel und Farben.</summary>
internal enum RavenVoice
{
    /// <summary>Eine Stimme aus Narnia (Lob, Willkommen, Würdigung): goldener Rabe, grünes Siegel.</summary>
    Narnia,

    /// <summary>Die Königin lockt (Mahnruf nach Anzahl in Band II): Kronen-Medaillon, rotes Siegel, Antwort aus Narnia.</summary>
    Queen,

    /// <summary>Ein Mahnruf mit nur einer Stimme (Frostminuten, andere Bände): eisblauer Rabe, rotes Siegel.</summary>
    Frost,
}

/// <summary>Inhalt eines Briefes des Rabenboten.</summary>
/// <param name="Header">Kopfzeile, z. B. „Lob · 25 Minuten am Stück“ (wird in Versalien gesetzt).</param>
/// <param name="Message">Die eigentliche Botschaft.</param>
/// <param name="Source">Zeile darunter, z. B. „— Herr Tumnus · Der König von Narnia“ oder die Unterschrift der Königin.</param>
/// <param name="Lifetime">Wie lange der Brief sichtbar bleibt (pausiert, solange die Maus darüber ist).</param>
internal sealed record RavenLetter(string Header, string Message, string Source, TimeSpan Lifetime)
{
    /// <summary>Wer spricht (Standard: eine Stimme aus Narnia).</summary>
    public RavenVoice Voice { get; init; } = RavenVoice.Narnia;

    /// <summary>Kleine kursive Zeile unter der Kopfzeile, z. B. „nach 12:40 am Schlitten“.</summary>
    public string? Subline { get; init; }

    /// <summary>Antwort aus Narnia auf die Stimme der Königin.</summary>
    public string? ReplyText { get; init; }

    /// <summary>Wer antwortet, z. B. „— Herr Tumnus“.</summary>
    public string? ReplySource { get; init; }

    /// <summary>Mahnrufe kommen mit rotem Siegel und eisblauem Restzeit-Balken.</summary>
    public bool IsAdmonition => Voice != RavenVoice.Narnia;
}

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

    internal RavenToastWindow(RavenLetter letter)
    {
        ArgumentNullException.ThrowIfNull(letter);
        InitializeComponent();

        HeaderText.Text = letter.Header.ToUpper(CultureInfo.CurrentCulture);
        MessageText.Text = letter.Message;
        SourceText.Text = letter.Source;
        SourceText.Visibility = string.IsNullOrEmpty(letter.Source) ? Visibility.Collapsed : Visibility.Visible;
        if (!string.IsNullOrEmpty(letter.Subline))
        {
            SublineText.Text = letter.Subline;
            SublineText.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(letter.ReplyText))
        {
            ReplyText.Text = letter.ReplyText;
            ReplySourceText.Text = letter.ReplySource ?? string.Empty;
            ReplySourceText.Visibility = string.IsNullOrEmpty(letter.ReplySource) ? Visibility.Collapsed : Visibility.Visible;
            ReplyPanel.Visibility = Visibility.Visible;
        }

        ApplyVoice(letter);
        _lifetime = letter.Lifetime;

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
        Slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(60, FlyDuration));
        Root.BeginAnimation(OpacityProperty, fade);
    }

    /// <summary>Medaillon, Siegel und Tintenfarben passend zur Stimme setzen (einmalig beim Erzeugen).</summary>
    private void ApplyVoice(RavenLetter letter)
    {
        if (!letter.IsAdmonition)
        {
            return;   // Standard aus dem XAML: goldener Rabe, grünes Siegel, Goldtinte
        }

        // Mahnrufe: rotes Siegel mit Schneeflocke, Kopf in Narnia-Rot, Restzeit in tiefem Eisblau.
        Seal.SealBrush = Themed("NarniaRedBrush");
        Seal.Glyph = (Geometry)FindResource("NarniaSnowflakeGeometry");
        Seal.GlyphBrush = Themed("WaxGlyphBrush");
        HeaderText.Foreground = Themed("InkRedBrush");
        Lifetime.Background = Themed("IceDeepBrush");

        // Das Medaillon wird eisblau mit tiefblauem Ring.
        var ice = Themed("IceMedallionBrush");
        var iceInk = Themed("InkFrostBrush");
        MedallionDisc.Fill = ice;
        MedallionDisc.Stroke = iceInk;
        MedallionRing.Stroke = iceInk;

        if (letter.Voice == RavenVoice.Queen)
        {
            // Die Stimme der Königin: kursiv in Eistinte, darunter ihre Unterschrift (schlicht, kleiner).
            RavenFigure.Visibility = Visibility.Collapsed;
            QueenFigure.Visibility = Visibility.Visible;
            MessageText.FontStyle = FontStyles.Italic;
            MessageText.FontSize = 18;
            MessageText.LineHeight = 24.5;
            MessageText.Foreground = iceInk;
            SourceText.FontStyle = FontStyles.Normal;
            SourceText.FontSize = 13;
            SourceText.Margin = new Thickness(0, 5, 0, 0);
            SourceText.Foreground = Themed("SignatureBrush");
        }
        else
        {
            RavenWing.Stroke = Themed("IceWingBrush");
            RavenEye.Fill = ice;
        }
    }

    private Brush Themed(string key) => (Brush)FindResource(key);

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
        Slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(0, FlyDuration) { EasingFunction = ease });
        Root.BeginAnimation(OpacityProperty, new DoubleAnimation(1, FlyDuration));

        var countdown = new DoubleAnimation(1, 0, new Duration(_lifetime));
        Timeline.SetDesiredFrameRate(countdown, CountdownFrameRate);
        countdown.Completed += (_, _) => FlyOut();
        _lifetimeClock = countdown.CreateClock();
        LifetimeScale.ApplyAnimationClock(ScaleTransform.ScaleXProperty, _lifetimeClock);
    }
}
