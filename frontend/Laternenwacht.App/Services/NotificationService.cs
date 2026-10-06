using System.Media;
using Laternenwacht.App.Views;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Services;

/// <summary>
/// Überbringt Botschaften als Push-Benachrichtigung ("Rabenbote"). Es ist stets höchstens eine sichtbar.
/// Mahnrufe kommen mit Ton und in Eisblau, Lob leise in Laternenorange – damit es die
/// Konzentration nicht seinerseits stört.
/// </summary>
internal sealed class NotificationService
{
    public static readonly TimeSpan AdmonitionLifetime = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan PraiseLifetime = TimeSpan.FromSeconds(8);

    private RavenToastWindow? _current;

    /// <summary>Mahnruf bei vielen Ablenkungen.</summary>
    public void Show(Admonition admonition)
    {
        ArgumentNullException.ThrowIfNull(admonition);
        Present("Mahnruf", admonition.Text, ChronicleBooks.Title(admonition.Book), "!",
            AdmonitionLifetime, positive: false);
        SystemSounds.Asterisk.Play();
    }

    /// <summary>Lob oder Willkommensgruß eines Bewohners des Reiches.</summary>
    public void ShowEncouragement(string header, Encouragement encouragement)
    {
        ArgumentNullException.ThrowIfNull(encouragement);
        Present(header, encouragement.Text, $"{encouragement.Speaker} · {ChronicleBooks.Title(encouragement.Book)}",
            Initial(encouragement.Speaker), PraiseLifetime, positive: true);
    }

    /// <summary>Freie positive Botschaft (z. B. Würdigung einer vollendeten Wacht).</summary>
    public void ShowPraise(string header, string message, string source) =>
        Present(header, message, source, "✓", PraiseLifetime, positive: true);

    /// <summary>Anfangsbuchstabe des Namens, Artikel übersprungen ("der Weihnachtsmann" → "W").</summary>
    private static string Initial(string speaker)
    {
        var letter = speaker.FirstOrDefault(char.IsUpper);
        return letter == default ? speaker[..1].ToUpperInvariant() : letter.ToString();
    }

    private void Present(string header, string message, string source, string avatar, TimeSpan lifetime, bool positive)
    {
        _current?.FlyOut();
        var toast = new RavenToastWindow(header, message, source, avatar, lifetime, positive);
        toast.Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, toast))
            {
                _current = null;
            }
        };
        _current = toast;
        toast.Show();
    }
}
