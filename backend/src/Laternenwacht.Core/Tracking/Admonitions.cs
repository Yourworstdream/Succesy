using System.Collections.Frozen;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>Ein augenzwinkernder Mahnruf, der bei Erreichen einer Schwelle erscheint.</summary>
/// <param name="Key">Eindeutiger Schlüssel der Schwelle, damit jeder Mahnruf nur einmal je Wacht erscheint.</param>
/// <param name="Text">Der anzuzeigende Spruch.</param>
/// <param name="Book">Das Buch der Chroniken, aus dessen Welt der Spruch stammt.</param>
public sealed record Admonition(string Key, string Text, ChronicleBook Book);

/// <summary>
/// Mahnrufe des Reiches: humorvolle Hinweise, wenn die Ablenkung überhandnimmt.
/// Ausgelöst wird entweder durch die Anzahl der Verlockungen oder durch die Minuten im Frost.
/// Jedes der sieben Bücher hat einen eigenen Satz Sprüche (alles eigene Formulierungen).
/// </summary>
public static class Admonitions
{
    /// <summary>Schwellen nach Anzahl der Verlockungen (Index 0–6 in den Spruchlisten).</summary>
    public static IReadOnlyList<int> CountThresholds { get; } = [3, 5, 10, 15, 20, 30, 50];

    /// <summary>Schwellen nach Minuten im Frost (Index 7–10 in den Spruchlisten).</summary>
    public static IReadOnlyList<int> FrostMinuteThresholds { get; } = [5, 10, 20, 45];

    private static readonly FrozenDictionary<ChronicleBook, string[]> Sayings = new Dictionary<ChronicleBook, string[]>
    {
        [ChronicleBook.MagiciansNephew] =
        [
            "Ein gelber Ring blitzt auf – Onkel Andrew hofft, du probierst ihn gleich aus.",
            "Der Wald zwischen den Welten ist schön, aber dort vergisst man so leicht, warum man gekommen ist.",
            "Jemand hat schon wieder die Glocke in Charn angeschlagen. Warst du das?",
            "Digory, Polly – die Teiche warten nicht ewig. Zurück an die Arbeit!",
            "Kaiserin Jadis nickt anerkennend: So viel Zerstreuung hat nicht einmal Charn zu Fall gebracht.",
            "Gerade wird eine ganze Welt gesungen – und du verpasst den Refrain.",
            "Onkel Andrew notiert dich als gelungenes Experiment: Proband verschwindet regelmäßig in andere Welten.",
            "Fünf Minuten im Wald zwischen den Welten – erst dösen die Gedanken, dann der Rest.",
            "Zehn Minuten Frost: Ein geflügeltes Pferd hätte dich längst über alle Berge getragen.",
            "Zwanzig Minuten! Der Apfel aus dem Garten im Westen wird schon braun.",
            "Dreiviertel Stunde – auch Charn war einst eine blühende Stadt, bevor man sie vergaß.",
        ],
        [ChronicleBook.LionWitchWardrobe] =
        [
            "Ein Faun räuspert sich hinter dem Laternenpfahl: „Nur ein kleiner Umweg, gewiss?“",
            "Die Weiße Hexe lässt grüßen – und reicht dir ein Stück türkischen Honig.",
            "Die Biber tuscheln schon: „Da ist wieder jemand vom Pfad abgekommen …“",
            "Edmund lässt ausrichten: Er kennt sich mit Verlockungen aus – und rät dringend ab.",
            "Herrscher von Cair Paravel, Ihr gefährdet Euer Königreich mit Eurem Müßiggang!",
            "Der Steintisch bebt. Selbst die Dryaden schütteln ihre Zweige über dich.",
            "Hundert Jahre Winter wären kürzer gewesen als diese Wacht. Irgendwo seufzt ein Löwe.",
            "Fünf Minuten im Frost – deine Laterne zittert im Wind.",
            "Zehn Minuten Frost! Die Eiszapfen an deinem Schreibtisch werden länger.",
            "Zwanzig Minuten Frost. Man munkelt, du habest den Rückweg durch den Schrank vergessen.",
            "Dreiviertel Stunde Frost: immer Winter, nie Weihnachten – und kein Geschenk für dich.",
        ],
        [ChronicleBook.HorseAndHisBoy] =
        [
            "Bree schnaubt: „Ein sprechendes Pferd lässt sich nicht so leicht ablenken. Du offenbar schon.“",
            "In Tashbaan erzählt man sich bereits Geschichten von deinen Umwegen.",
            "Der Tisroc – möge er ewig leben – fragt, ob du ewig trödeln möchtest.",
            "Aravis hebt eine Braue. Eine Tarkheena wäre längst in Archenland angekommen.",
            "Prinz Rabadash wurde für weniger Torheit in einen Esel verwandelt. Nur so als Hinweis.",
            "Die Wüste ist weit, die Wasserschläuche sind leer – und du schaust schon wieder woanders hin.",
            "Hwin flüstert sanft: „Wir könnten auch einfach … weiterreiten?“",
            "Fünf Minuten Frost – selbst in Kalormens Hitze ein kühler Hauch.",
            "Zehn Minuten! Der Einsiedler am Rand der Wüste hat schon den Tee aufgesetzt.",
            "Zwanzig Minuten Frost. Archenland rückt nicht näher, wenn man nicht reitet.",
            "Dreiviertel Stunde – Bree hätte in der Zeit dreimal von seinen Schlachten erzählt.",
        ],
        [ChronicleBook.PrinceCaspian] =
        [
            "Trumpkin brummt: „Bei allen Bärten – schon wieder abgeschweift?“",
            "König Miraz freut sich über jede Minute, die du nicht für die alten Narnianen kämpfst.",
            "Reepicheep zückt den Degen: Eine Maus von Ehre duldet keine Tändelei!",
            "Der Dachs Trüffeljäger vergisst nichts. Auch diese Ablenkungen nicht.",
            "Prinz Kaspian, die Telmarer stehen vor den Toren – und Ihr scrollt?",
            "Susans Horn liegt bereit. Doch zum Ablenken bläst man es nicht.",
            "Efeu überwuchert die Ruinen von Cair Paravel – so lange dauert diese Wacht schon.",
            "Fünf Minuten Frost – die Bäume schlafen wieder ein.",
            "Zehn Minuten! Nikabrik schlägt vor, es mit dunkleren Mächten zu versuchen. Lieber nicht.",
            "Zwanzig Minuten Frost. Selbst ein Zwerg ohne Karte hätte inzwischen den Weg gefunden.",
            "Dreiviertel Stunde – die Telmarer haben bereits eine Brücke über deinen Fluss gebaut.",
        ],
        [ChronicleBook.DawnTreader] =
        [
            "Eustachius schreibt ins Tagebuch: „Heute war wieder jemand abgelenkt. Ausnahmsweise nicht ich.“",
            "Die Morgenröte setzt die Segel – ohne dich, wenn das so weitergeht.",
            "Auf der Insel der Stimmen kichern Unsichtbare über deine Umwege.",
            "Vorsicht: Wer zu lange über Drachenschätzen brütet, wird selbst zum Drachen.",
            "König Kaspian, der Kurs führt nach Osten – nicht in jede Bucht der Verlockung!",
            "Die Seeschlange umkreist dein Schiff. Sie hat Zeit. Du nicht.",
            "Reepicheep paddelt allein zum Ende der Welt. Er hat aufgehört, auf dich zu warten.",
            "Fünf Minuten Flaute – die Segel hängen schlaff.",
            "Zehn Minuten! Der Zauberer klappt sein Buch zu und schaut dich vorwurfsvoll an.",
            "Zwanzig Minuten Frost. Die Dunkle Insel lässt grüßen – dort werden Träume wahr, auch die schlechten.",
            "Dreiviertel Stunde – das Meer wird süß, die Lilien blühen, und du hast noch nicht einmal abgelegt.",
        ],
        [ChronicleBook.SilverChair] =
        [
            "Ein griesgrämiger Moorbewohner prophezeit: „Das wird nichts mit dem Fokus. Versuchen wir es trotzdem.“",
            "Denk an die Zeichen! Keines davon lautete „Nur mal kurz nachsehen“.",
            "Die Dame im grünen Gewand lächelt. Sie liebt es, wenn man vergisst, was man wollte.",
            "Die Riesen von Harfang laden dich zum Herbstfest ein. Du stehst auf der Speisekarte.",
            "Prinz Rilian, Ihr sitzt schon wieder im silbernen Sessel der Zerstreuung!",
            "Im Unterland ist es dunkel, still – und voller Leute, die ihre Aufgabe vergessen haben.",
            "Jill und Eustachius kennen die Zeichen auswendig. Du offenbar nur die Verlockungen.",
            "Fünf Minuten Frost – der Nordwind pfeift über die Moore.",
            "Zehn Minuten! Der grüne Zauberduft wirkt bereits. Kopf schütteln, weiterarbeiten!",
            "Zwanzig Minuten Frost. Selbst die Erdmännchen im Unterland arbeiten fleißiger.",
            "Dreiviertel Stunde – es gibt eine Sonne über Narnia, auch wenn das Unterland dir anderes einredet.",
        ],
        [ChronicleBook.LastBattle] =
        [
            "Der Affe in der Hütte verkauft dir Ablenkung als „ganz wichtige Sache“. Glaub ihm kein Wort.",
            "Ein Esel im Löwenfell ist noch kein Löwe – und ein kurzer Blick ist selten nur ein kurzer.",
            "König Tirian runzelt die Stirn: Narnia verteidigt sich nicht von allein.",
            "Das Einhorn Juwel scharrt mit dem Huf. Es mag keine Zauderer.",
            "Die Zwerge sind für die Zwerge – und du bist offenbar für die Verlockungen.",
            "Hinter der Stalltür wartet Tash. Er ernährt sich von verlorener Zeit.",
            "Die Sterne fallen bereits vom Himmel. Vielleicht wäre jetzt ein guter Moment, anzufangen?",
            "Fünf Minuten Frost – über Narnia ziehen dunkle Wolken auf.",
            "Zehn Minuten! Am Waldrand fallen schon die sprechenden Bäume.",
            "Zwanzig Minuten Frost. Weiter hinauf und weiter hinein – nicht weiter weg!",
            "Dreiviertel Stunde – die letzte Nacht über Narnia bricht an. Rette wenigstens deine Wacht.",
        ],
    }.ToFrozenDictionary();

    /// <summary>Alle Sprüche eines Buches in Schwellenreihenfolge (erst Anzahl, dann Frostminuten).</summary>
    public static IReadOnlyList<string> For(ChronicleBook book) =>
        Sayings.TryGetValue(book, out var lines) ? lines : throw new ArgumentOutOfRangeException(nameof(book));

    /// <summary>
    /// Liefert den höchsten neu erreichten, noch nicht gezeigten Mahnruf oder <c>null</c>.
    /// Übersprungene niedrigere Schwellen gelten danach ebenfalls als gezeigt,
    /// damit nicht mehrere Sprüche nacheinander nachgereicht werden.
    /// </summary>
    /// <param name="book">Gewähltes Buch; bei <see cref="ChronicleBook.All"/> wechselt das Buch je Schwelle.</param>
    /// <param name="seed">Startwert für die Buchauswahl im gemischten Modus (z. B. je Wacht zufällig).</param>
    public static Admonition? Next(int distractionCount, TimeSpan frost, ISet<string> alreadyShown,
        ChronicleBook book = ChronicleBook.LionWitchWardrobe, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(alreadyShown);
        if (!Enum.IsDefined(book))
        {
            throw new ArgumentOutOfRangeException(nameof(book));
        }

        Admonition? result = null;

        for (var i = 0; i < CountThresholds.Count; i++)
        {
            if (distractionCount >= CountThresholds[i] && alreadyShown.Add($"count:{CountThresholds[i]}"))
            {
                result = Create($"count:{CountThresholds[i]}", i, book, seed);
            }
        }

        for (var i = 0; i < FrostMinuteThresholds.Count; i++)
        {
            var minutes = FrostMinuteThresholds[i];
            if (frost >= TimeSpan.FromMinutes(minutes) && alreadyShown.Add($"frost:{minutes}"))
            {
                result = Create($"frost:{minutes}", CountThresholds.Count + i, book, seed);
            }
        }

        return result;
    }

    private static Admonition Create(string key, int index, ChronicleBook book, int seed)
    {
        var source = book == ChronicleBook.All
            ? ChronicleBooks.Volumes[(int)((uint)(seed + index) % (uint)ChronicleBooks.Volumes.Count)]
            : book;
        return new Admonition(key, Sayings[source][index], source);
    }
}
