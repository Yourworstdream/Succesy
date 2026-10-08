using System.Collections.Frozen;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>Ein augenzwinkernder Mahnruf, der bei Erreichen einer Schwelle erscheint.</summary>
/// <param name="Key">Eindeutiger Schlüssel der Schwelle, damit jeder Mahnruf nur einmal je Wacht erscheint.</param>
/// <param name="Text">Der anzuzeigende Spruch.</param>
/// <param name="Book">Das Buch der Chroniken, aus dessen Welt der Spruch stammt.</param>
public sealed record Admonition(string Key, string Text, ChronicleBook Book)
{
    /// <summary>
    /// Die Antwort aus Narnia auf einen Mahnruf der Königin, sonst <c>null</c>.
    /// Gesetzt nur bei <see cref="ChronicleBook.LionWitchWardrobe"/> und den Anzahl-Schwellen: Dann spricht in
    /// <see cref="Text"/> die Königin (Unterschrift: <see cref="Admonitions.QueenSignature"/>), und die Königin
    /// hat nie das letzte Wort.
    /// </summary>
    public Encouragement? Reply { get; init; }
}

/// <summary>
/// Mahnrufe des Reiches: humorvolle Hinweise, wenn die Ablenkung überhandnimmt.
/// Ausgelöst wird entweder durch die Anzahl der Verlockungen oder durch die Minuten im Frost.
/// Jedes der sieben Bücher hat einen eigenen Satz Sprüche (alles eigene Formulierungen).
/// In „Der König von Narnia“ sprechen bei den Anzahl-Schwellen die Königin und eine Stimme aus Narnia,
/// die ihr antwortet (<see cref="Admonition.Reply"/>).
/// </summary>
public static class Admonitions
{
    /// <summary>Unterschrift unter der Stimme der Königin (bei Mahnrufen mit <see cref="Admonition.Reply"/>).</summary>
    public const string QueenSignature = "Jadis, die sich Königin von Narnia nennt";

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
            // Anzahl-Schwellen: die Stimme der Königin (Antworten siehe Replies)
            "Noch ein Stück? Nimm ruhig, meine Schachtel wird nie leer.",
            "Fünfmal warst du schon bei mir. Aus dir könnte ein Prinz werden.",
            "Lass deine Gefährten warten. In meinem Haus zwischen den Hügeln ist es viel gemütlicher.",
            "Fünfzehn Stück. Komm doch mit in meinen Hof, dort hat es niemand mehr eilig.",
            "Zwanzig Stück! Honig gibt es ab jetzt keinen mehr, nur trockenes Brot. Bleibst du trotzdem?",
            "Dreißig! Mein Zwerg spannt schon die Rentiere für dich an.",
            "Fünfzig Stück. Von hier aus sieht man deine Laterne kaum noch.",

            // Frost-Schwellen: Stimmen aus Narnia
            "Fünf Minuten im Frost. An deiner Laterne hängt der erste Eiszapfen – noch ist er ganz klein.",
            "Zehn Minuten! Maugrims Wölfe streifen schon durch den Wald. Folge deiner Spur zurück zur Laterne, noch ist sie gut zu sehen.",
            "Zwanzig Minuten Frost. Im Hof der Königin wird es eng zwischen den Steinfiguren. Du gehörst nicht dorthin – komm heim.",
            "Eine Dreiviertelstunde im Winter der Königin. Und doch tropft es irgendwo schon von den Ästen. Für die Rückkehr ist es nie zu spät.",
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
            "Riepiepich zückt den Degen: Eine Maus von Ehre duldet keine Tändelei!",
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
            "Riepiepich paddelt allein zum Ende der Welt. Er hat aufgehört, auf dich zu warten.",
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
            "Zwanzig Minuten Frost. König Tirian hält die Stellung allein – lass ihn nicht länger warten.",
            "Dreiviertel Stunde – die letzte Nacht über Narnia bricht an. Rette wenigstens deine Wacht.",
        ],
    }.ToFrozenDictionary();

    /// <summary>
    /// Antworten aus Narnia auf die Mahnrufe der Königin, je Anzahl-Schwelle eine (Index 0–6).
    /// Nur „Der König von Narnia“ hat sie; die übrigen Bücher bleiben ohne Antwort.
    /// </summary>
    private static readonly FrozenDictionary<ChronicleBook, (string Text, string Speaker)[]> Replies =
        new Dictionary<ChronicleBook, (string Text, string Speaker)[]>
        {
            [ChronicleBook.LionWitchWardrobe] =
            [
                ("Je mehr man davon nimmt, desto hungriger wird man. Steh lieber auf.", "Herr Tumnus"),
                ("Ihre Kronen sind aus Eis. Die echten Throne stehen in Cair Paravel.", "Herr Biber"),
                ("Edmund ist abends zu ihr gelaufen. Du musst das nicht – deine Gefährten sind ganz nah.", "Frau Biber"),
                ("Die dort stehen, sind aus Stein. Du nicht – also los, zurück zu deinen Gefährten.", "Lucy"),
                ("Genau so ging es Edmund. Was sie verspricht, hält sie nicht.", "Peter"),
                ("Lass ihn spannen. Ihr Schlitten bleibt bald im Matsch stecken, der Frühling ist näher, als sie glaubt.", "Herr Biber"),
                ("Ich sehe dich. Ein Schritt zurück ins Licht genügt.", "Aslan"),
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
        var reply = Replies.TryGetValue(source, out var replies) && index < replies.Length
            ? new Encouragement(replies[index].Text, replies[index].Speaker, source)
            : null;
        return new Admonition(key, Sayings[source][index], source) { Reply = reply };
    }
}
