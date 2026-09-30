namespace Laternenwacht.Core.Tracking;

/// <summary>Ein augenzwinkernder Mahnruf, der bei Erreichen einer Schwelle erscheint.</summary>
/// <param name="Key">Eindeutiger Schlüssel der Schwelle, damit jeder Mahnruf nur einmal je Wacht erscheint.</param>
/// <param name="Text">Der anzuzeigende Spruch.</param>
public sealed record Admonition(string Key, string Text);

/// <summary>
/// Mahnrufe des Reiches: humorvolle Hinweise, wenn die Ablenkung überhandnimmt.
/// Ausgelöst wird entweder durch die Anzahl der Verlockungen oder durch die Minuten im Frost.
/// </summary>
public static class Admonitions
{
    private static readonly (int Count, string Text)[] ByCount =
    [
        (3, "Ein Faun räuspert sich hinter dem Laternenpfahl: „Nur ein kleiner Umweg, gewiss?“"),
        (5, "Die Weiße Hexe lässt grüßen – und reicht dir ein Stück türkischen Honig."),
        (10, "Die Biber tuscheln schon: „Da ist wieder jemand vom Pfad abgekommen …“"),
        (15, "Ritter des Reiches, Euer Schwert rostet, während Ihr Verlockungen nachjagt!"),
        (20, "Herrscher von Cair Paravel, Ihr gefährdet Euer Königreich mit Eurem Müßiggang!"),
        (30, "Der Steintisch bebt. Selbst die Dryaden schütteln ihre Zweige über dich."),
        (50, "Hundert Jahre Winter wären kürzer gewesen als diese Wacht. Irgendwo seufzt ein Löwe."),
    ];

    private static readonly (int Minutes, string Text)[] ByFrostMinutes =
    [
        (5, "Fünf Minuten im Frost – deine Laterne zittert im Wind."),
        (10, "Zehn Minuten Frost! Die Eiszapfen an deinem Schreibtisch werden länger."),
        (20, "Zwanzig Minuten Frost. Man munkelt, du habest den Rückweg durch den Schrank vergessen."),
        (45, "Dreiviertel Stunde Frost. Die Schneekönigin erwägt, dir einen Thron anzubieten."),
    ];

    /// <summary>
    /// Liefert den höchsten neu erreichten, noch nicht gezeigten Mahnruf oder <c>null</c>.
    /// Übersprungene niedrigere Schwellen gelten danach ebenfalls als gezeigt,
    /// damit nicht mehrere Sprüche nacheinander nachgereicht werden.
    /// </summary>
    public static Admonition? Next(int distractionCount, TimeSpan frost, ISet<string> alreadyShown)
    {
        ArgumentNullException.ThrowIfNull(alreadyShown);

        Admonition? result = null;

        foreach (var (count, text) in ByCount.Where(c => distractionCount >= c.Count))
        {
            var key = $"count:{count}";
            if (alreadyShown.Add(key))
            {
                result = new Admonition(key, text);
            }
        }

        foreach (var (minutes, text) in ByFrostMinutes.Where(f => frost >= TimeSpan.FromMinutes(f.Minutes)))
        {
            var key = $"frost:{minutes}";
            if (alreadyShown.Add(key))
            {
                result = new Admonition(key, text);
            }
        }

        return result;
    }
}
