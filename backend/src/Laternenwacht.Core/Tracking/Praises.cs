using System.Collections.Frozen;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>Eine erreichte Fokus-Serie (z. B. 25 Minuten am Stück ohne Ablenkung).</summary>
/// <param name="Minutes">Die erreichte Schwelle in Minuten.</param>
/// <param name="Index">Position der Schwelle in <see cref="Praises.StreakMilestones"/>.</param>
public sealed record FocusStreakReached(int Minutes, int Index);

/// <summary>
/// Lob für gute Konzentration: Bewohner des Reiches würdigen lange Fokus-Serien.
/// Je Buch ein Lob pro Schwelle. Alle Texte sind eigene Formulierungen.
/// </summary>
public static class Praises
{
    /// <summary>Schwellen ununterbrochener Fokuszeit in Minuten.</summary>
    public static IReadOnlyList<int> StreakMilestones { get; } = [10, 25, 45, 60, 90];

    private static readonly FrozenDictionary<ChronicleBook, (string Text, string Speaker)[]> Lines =
        new Dictionary<ChronicleBook, (string Text, string Speaker)[]>
        {
            [ChronicleBook.MagiciansNephew] =
            [
                ("Zehn Minuten ohne Abschweifen – nicht einmal der Wald zwischen den Welten konnte dich einlullen.", "Polly"),
                ("Fünfundzwanzig Minuten! So standhaft war ich nicht einmal vor der Glocke in Charn.", "Digory"),
                ("Dreiviertel Stunde im Licht. Aus solchen Samen wachsen Laternenpfähle.", "Kutscher Frank"),
                ("Eine ganze Stunde! Selbst die Sterne über der jungen Welt singen für dich.", "das geflügelte Pferd"),
                ("Anderthalb Stunden. Das ist der Stoff, aus dem Könige gemacht werden.", "Kutscher Frank"),
            ],
            [ChronicleBook.LionWitchWardrobe] =
            [
                ("Zehn Minuten ohne Verlockung – nicht einmal türkischer Honig hätte dich erwischt.", "Lucy"),
                ("Fünfundzwanzig Minuten! Der Schnee unter deinem Laternenpfahl beginnt zu schmelzen.", "Herr Tumnus"),
                ("Dreiviertel Stunde – das ist echter Biberfleiß. Ich bin stolz auf dich.", "Herr Biber"),
                ("Eine Stunde im Licht! Dafür lege ich dir glatt ein Geschenk unter den Baum.", "der Weihnachtsmann"),
                ("Anderthalb Stunden. Auf Cair Paravel wird man von dieser Wacht noch lange erzählen.", "Peter, der Hochkönig"),
            ],
            [ChronicleBook.HorseAndHisBoy] =
            [
                ("Zehn Minuten im gestreckten Galopp – und kein einziges Mal gestolpert!", "Bree"),
                ("Fünfundzwanzig Minuten. Ruhig und stetig – so kommt man durch jede Wüste.", "Hwin"),
                ("Dreiviertel Stunde! Selbst eine Tarkheena nickt da anerkennend.", "Aravis"),
                ("Eine Stunde ohne Umweg. So erreicht man Archenland noch vor der Nacht.", "Shasta"),
                ("Anderthalb Stunden! In Archenland gäbe man dir zu Ehren ein Fest.", "König Lune"),
            ],
            [ChronicleBook.PrinceCaspian] =
            [
                ("Zehn Minuten am Stück. Bei meinem Bart, das kann sich sehen lassen!", "Trumpkin"),
                ("Fünfundzwanzig Minuten – treu und beständig, wie es sich für einen Dachs gehört.", "Trüffeljäger"),
                ("Dreiviertel Stunde ohne Wanken. Eine Maus von Ehre salutiert dir!", "Riepiepich"),
                ("Eine Stunde! Mit solchen Gefährten gewinnen wir Narnia zurück.", "Prinz Kaspian"),
                ("Anderthalb Stunden. Das schreibe ich in die Geschichtsbücher, mein Schüler.", "Doktor Cornelius"),
            ],
            [ChronicleBook.DawnTreader] =
            [
                ("Zehn Minuten mit vollem Wind in den Segeln – weiter so!", "Kapitän Drinian"),
                ("Fünfundzwanzig Minuten. Ich gebe es ungern zu, aber: beeindruckend.", "Eustachius"),
                ("Dreiviertel Stunde auf Kurs – kein einziger Abstecher zu einer Insel der Verlockung.", "Lucy"),
                ("Eine Stunde! So segelt man bis ans Ende der Welt.", "Riepiepich"),
                ("Anderthalb Stunden. Die Morgenröte ist stolz, dich an Bord zu haben.", "König Kaspian"),
            ],
            [ChronicleBook.SilverChair] =
            [
                ("Zehn Minuten – und kein einziges Zeichen vergessen. Gut gemacht!", "Jill"),
                ("Fünfundzwanzig Minuten … das läuft verdächtig gut. Aber ich will nichts beschreien.", "ein griesgrämiger Moorbewohner"),
                ("Dreiviertel Stunde! Kein silberner Sessel hält dich fest.", "Prinz Rilian"),
                ("Eine Stunde im Licht – das Unterland hat keine Macht über dich.", "Eustachius"),
                ("Anderthalb Stunden. Selbst der Moorbewohner musste lächeln. Fast.", "Jill"),
            ],
            [ChronicleBook.LastBattle] =
            [
                ("Zehn Minuten treu auf dem Posten – Narnia dankt dir.", "König Tirian"),
                ("Fünfundzwanzig Minuten! Du bist nicht nur für dich selbst da – du bist für die Sache da.", "Poggin, der treue Zwerg"),
                ("Dreiviertel Stunde ohne Wanken. Mit dir ritte ich in jede Schlacht.", "Juwel, das Einhorn"),
                ("Eine Stunde! So hält man die letzte Stellung.", "König Tirian"),
                ("Anderthalb Stunden – weiter hinauf und weiter hinein!", "Juwel, das Einhorn"),
            ],
        }.ToFrozenDictionary();

    /// <summary>Lob für eine erreichte Fokus-Serie; bei <see cref="ChronicleBook.All"/> wechselt das Buch je Schwelle.</summary>
    public static Encouragement ForStreak(ChronicleBook book, int milestoneIndex, int seed = 0)
    {
        if (!Enum.IsDefined(book))
        {
            throw new ArgumentOutOfRangeException(nameof(book));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(milestoneIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(milestoneIndex, StreakMilestones.Count);

        var source = book == ChronicleBook.All
            ? ChronicleBooks.Volumes[(int)((uint)(seed + milestoneIndex) % (uint)ChronicleBooks.Volumes.Count)]
            : book;
        var (text, speaker) = Lines[source][milestoneIndex];
        return new Encouragement(text, speaker, source);
    }
}
