using System.Globalization;
using System.Media;
using Laternenwacht.App.Views;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Services;

/// <summary>
/// Überbringt Botschaften als Brief des Rabenboten (Push-Benachrichtigung). Es ist stets höchstens einer sichtbar.
/// Mahnrufe kommen mit Ton und rotem Siegel – in „Der König von Narnia“ spricht dabei die Königin, und eine Stimme
/// aus Narnia antwortet ihr. Lob und Willkommen kommen leise mit grünem Siegel, damit sie die Konzentration nicht
/// ihrerseits stören.
/// </summary>
internal sealed class NotificationService
{
    public static readonly TimeSpan AdmonitionLifetime = TimeSpan.FromSeconds(10);

    /// <summary>Königin und Antwort sind zwei Absätze – etwas mehr Zeit zum Lesen.</summary>
    public static readonly TimeSpan QueenLifetime = TimeSpan.FromSeconds(14);
    public static readonly TimeSpan PraiseLifetime = TimeSpan.FromSeconds(8);

    private const string CountKeyPrefix = "count:";
    private const string FrostKeyPrefix = "frost:";

    private RavenToastWindow? _current;

    /// <summary>
    /// Mahnruf bei vielen Ablenkungen. Mit <see cref="Admonition.Reply"/> spricht die Königin (Unterschrift
    /// <see cref="Admonitions.QueenSignature"/>), und darunter steht die Antwort aus Narnia.
    /// </summary>
    public void Show(Admonition admonition)
    {
        ArgumentNullException.ThrowIfNull(admonition);
        var header = AdmonitionHeader(admonition.Key);
        RavenLetter letter = admonition.Reply is { } reply
            ? new RavenLetter(header, admonition.Text, "— " + Admonitions.QueenSignature, QueenLifetime)
            {
                Voice = RavenVoice.Queen,
                ReplyText = reply.Text,
                ReplySource = "— " + reply.Speaker,
            }
            : new RavenLetter(header, admonition.Text, "— " + ChronicleBooks.Title(admonition.Book), AdmonitionLifetime)
            {
                Voice = RavenVoice.Frost,
            };

        Present(letter);
        SystemSounds.Asterisk.Play();
    }

    /// <summary>„Willkommen zurück im Licht“ nach einer Ablenkung, mit der Dauer am Schlitten.</summary>
    public void ShowWelcome(Encouragement encouragement, TimeSpan absence)
    {
        ArgumentNullException.ThrowIfNull(encouragement);
        Present(new RavenLetter("Willkommen zurück im Licht", encouragement.Text, Signed(encouragement), PraiseLifetime)
        {
            Subline = $"nach {TimeFormat.Clock(absence)} am Schlitten",
        });
    }

    /// <summary>Lob für eine lange Fokus-Serie („Lob · 25 Minuten am Stück“).</summary>
    public void ShowStreakPraise(int minutes, Encouragement encouragement)
    {
        ArgumentNullException.ThrowIfNull(encouragement);
        Present(new RavenLetter(string.Create(CultureInfo.InvariantCulture, $"Lob · {minutes} Minuten am Stück"),
            encouragement.Text, Signed(encouragement), PraiseLifetime));
    }

    /// <summary>Lob oder Gruß eines Bewohners des Reiches mit freier Kopfzeile.</summary>
    public void ShowEncouragement(string header, Encouragement encouragement)
    {
        ArgumentNullException.ThrowIfNull(encouragement);
        Present(new RavenLetter(header, encouragement.Text, Signed(encouragement), PraiseLifetime));
    }

    /// <summary>Freie positive Botschaft (z. B. Würdigung einer vollendeten Wacht).</summary>
    public void ShowPraise(string header, string message, string source) =>
        Present(new RavenLetter(header, message, "— " + source, PraiseLifetime));

    /// <summary>
    /// Kopfzeile aus dem Schlüssel der Schwelle: „count:3“ → „Mahnruf · 3 Stück Türkischer Honig“,
    /// „frost:10“ → „Mahnruf · 10 Minuten Frost“; Unbekanntes bleibt schlicht „Mahnruf“.
    /// </summary>
    internal static string AdmonitionHeader(string key)
    {
        if (key.StartsWith(CountKeyPrefix, StringComparison.Ordinal))
        {
            return $"Mahnruf · {key[CountKeyPrefix.Length..]} Stück Türkischer Honig";
        }

        return key.StartsWith(FrostKeyPrefix, StringComparison.Ordinal)
            ? $"Mahnruf · {key[FrostKeyPrefix.Length..]} Minuten Frost"
            : "Mahnruf";
    }

    /// <summary>„— Herr Tumnus · Der König von Narnia“.</summary>
    private static string Signed(Encouragement encouragement) =>
        $"— {encouragement.Speaker} · {ChronicleBooks.Title(encouragement.Book)}";

    private void Present(RavenLetter letter)
    {
        _current?.FlyOut();
        var toast = new RavenToastWindow(letter);
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
