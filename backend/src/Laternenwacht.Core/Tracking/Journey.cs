using System.Globalization;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>Eine Station der Reise durch „Der König von Narnia“.</summary>
/// <param name="Number">Nummer der Station (1–10, in der Reihenfolge des Buches).</param>
/// <param name="Name">Kurzer Name, z. B. „Der Biberdamm“.</param>
/// <param name="Narration">Ein bis zwei Sätze, was an dieser Station im Buch geschieht (eigene Formulierung).</param>
public sealed record JourneyStation(int Number, string Name, string Narration);

/// <summary>
/// Aus welchem Versuchungskapitel Edmunds eine Verlockung erzählt wird – je nachdem, wo die Reise gerade steht.
/// </summary>
public enum TemptationChapter
{
    /// <summary>Der Schlitten der Königin: Edmund bekommt Türkischen Honig (Stationen 1–3).</summary>
    Sledge,

    /// <summary>Der nächtliche Weg vom Biberdamm zur Königin (Station 4).</summary>
    BeaverDam,

    /// <summary>Im Schloss der Königin gibt es nur noch trockenes Brot (ab Station 5).</summary>
    Castle,
}

/// <summary>Das Bild, mit dem eine Wacht in der Chronik endet.</summary>
public enum JourneyEnding
{
    /// <summary>Vollendet im Frühling: die Krönung in Cair Paravel.</summary>
    Coronation,

    /// <summary>Vollendet im Tauwetter: der Schlitten der Königin bleibt im Matsch stecken.</summary>
    Thaw,

    /// <summary>Vollendet im Winter: der Hof der Steinfiguren mit dem ersten goldenen Schein am Tor.</summary>
    StoneCourtyard,

    /// <summary>Abgebrochen: zurück durch den Schrank.</summary>
    Wardrobe,
}

/// <summary>Texte zum Abschlussbild einer Wacht.</summary>
/// <param name="Label">Kurzes Etikett in Großbuchstaben, z. B. „FRÜHLING“ oder „ABGEBROCHEN“.</param>
/// <param name="Title">Titel des Bildes, z. B. „Krönung in Cair Paravel“.</param>
/// <param name="Narration">Erzählabsatz unter dem Bild.</param>
public sealed record JourneyEndingText(string Label, string Title, string Narration);

/// <summary>
/// Die Reise einer Wacht durch „Der König von Narnia“: zehn Stationen in der Reihenfolge des Buches,
/// vom Laternenpfahl bis zu den vier Thronen von Cair Paravel.
/// </summary>
/// <remarks>
/// Ehrliche Mechanik: Die Station hängt allein am Verhältnis von gemessener zu geplanter Zeit
/// (<c>Station = 1 + floor(9 · Anteil)</c>, Cair Paravel erst bei 100 %). Abwesenheit zählt mit,
/// eine Rast hält die Reise an, und Ablenkung bremst sie nie – sie färbt nur die Geschichte.
/// Gerechnet wird exakt in ganzen Ticks, damit Stationsgrenze und „Nächste Station in …“ nie auseinanderlaufen.
/// Alle Texte sind eigene Formulierungen; nichts davon zitiert das Buch, eine Übersetzung oder einen Film.
/// </remarks>
public static class Journey
{
    /// <summary>Anzahl der Stationen.</summary>
    public const int StationCount = 10;

    /// <summary>Anzahl der Etappen zwischen den Stationen.</summary>
    private const int Legs = StationCount - 1;

    /// <summary>Längere Programmnamen werden im Verlockungstext gekürzt, damit die Zeile in die Oberfläche passt.</summary>
    private const int MaxProgramNameLength = 32;

    private const string UnknownTemptation = "Die Königin hält etwas Glänzendes in die Höhe.";

    /// <summary>Die zehn Stationen in der Reihenfolge des Buches.</summary>
    public static IReadOnlyList<JourneyStation> Stations { get; } =
    [
        new(1, "Der Laternenpfahl",
            "Lucy schiebt die Pelzmäntel beiseite und steht im Schnee. Am Laternenpfahl trifft sie Herrn Tumnus mit Schirm und Paketen."),
        new(2, "Der Schlitten der Königin",
            "Edmund folgt ihr durch den Schrank und begegnet der Königin auf ihrem Schlitten. Sie schenkt ihm Türkischen Honig, und er isst Stück um Stück."),
        new(3, "Die leere Höhle",
            "Alle vier sind in Narnia. Herrn Tumnus’ Höhle ist verwüstet, die Geheimpolizei der Königin hat ihn geholt. Ein Rotkehlchen führt zu Herrn Biber."),
        new(4, "Der Biberdamm",
            "Bei Herrn und Frau Biber hören die Kinder von Aslan und von vier Thronen in Cair Paravel. Am Abend schleicht Edmund hinaus in den Schnee."),
        new(5, "Der Hof der Königin",
            "Edmund schleicht zur Burg der Königin. In ihrem Hof stehen Wesen aus Stein, und statt Türkischem Honig bekommt er nur trockenes Brot und Wasser."),
        new(6, "Das Tauwetter",
            "Der Weihnachtsmann ist da, ihr Bann bricht. Als die Königin eine Festrunde versteinert, fühlt Edmund mit. Dann taut der Schnee, ihr Schlitten bleibt stecken."),
        new(7, "Der Steinerne Tisch",
            "Am Steinernen Tisch wartet Aslan. Peter besiegt Maugrim, Edmund wird befreit, spricht allein mit Aslan und bittet seine Geschwister um Verzeihung."),
        new(8, "Der Morgen am Tisch",
            "Aslan gibt sein Leben an Edmunds Stelle. Im Morgengrauen zerspringt der Tisch, Aslan lebt – und im Schloss der Königin weckt sein Atem die Steinfiguren."),
        new(9, "Die Schlacht",
            "Edmund zerschlägt den Zauberstab der Königin und wird schwer verwundet. Aslan besiegt die Königin, und Lucys Fläschchen heilt Edmund."),
        new(10, "Cair Paravel",
            "Vier Throne am Meer: Peter der Prächtige, Susan die Sanftmütige, Edmund der Gerechte und Lucy die Tapfere werden gekrönt."),
    ];

    /// <summary>Zurückgelegter Anteil der Reise (gemessen / geplant), begrenzt auf 0..1; ohne geplante Dauer 0.</summary>
    public static double Fraction(TimeSpan measured, TimeSpan planned) =>
        planned <= TimeSpan.Zero ? 0 : Math.Clamp(measured / planned, 0, 1);

    /// <summary>
    /// Nummer der erreichten Station (1–10): <c>1 + floor(9 · gemessen / geplant)</c>.
    /// Cair Paravel (10) wird erst erreicht, wenn die geplante Dauer voll ist.
    /// </summary>
    public static int StationNumber(TimeSpan measured, TimeSpan planned)
    {
        if (planned <= TimeSpan.Zero || measured <= TimeSpan.Zero)
        {
            return 1;
        }

        if (measured >= planned)
        {
            return StationCount;
        }

        // Exakt in Ticks, ohne Rundungsfehler an den Stationsgrenzen; der Quotient liegt hier zwischen 0 und 8.
        return 1 + (int)((Int128)measured.Ticks * Legs / planned.Ticks);
    }

    /// <summary>Die erreichte Station.</summary>
    public static JourneyStation StationAt(TimeSpan measured, TimeSpan planned) =>
        Stations[StationNumber(measured, planned) - 1];

    /// <summary>Lage einer Station auf dem Wegband (0 = Laternenpfahl, 1 = Cair Paravel); Nummern außerhalb 1–10 werden begrenzt.</summary>
    public static double PositionOf(int stationNumber) =>
        (Math.Clamp(stationNumber, 1, StationCount) - 1) / (double)Legs;

    /// <summary>
    /// Wie viel gemessene Zeit noch bis zur nächsten Station fehlt; <see cref="TimeSpan.Zero"/> in Cair Paravel
    /// oder ohne geplante Dauer. Nach genau dieser Zeit liefert <see cref="StationNumber"/> die nächste Station.
    /// </summary>
    public static TimeSpan UntilNextStation(TimeSpan measured, TimeSpan planned)
    {
        var station = StationNumber(measured, planned);
        if (planned <= TimeSpan.Zero || station >= StationCount)
        {
            return TimeSpan.Zero;
        }

        // Station n + 1 beginnt bei ceil(n · geplant / 9) Ticks.
        var nextStart = ((Int128)planned.Ticks * station + Legs - 1) / Legs;
        var travelled = measured > TimeSpan.Zero ? measured.Ticks : 0;
        return TimeSpan.FromTicks((long)(nextStart - travelled));
    }

    /// <summary>
    /// Aus welchem Versuchungskapitel Edmunds an dieser Station erzählt wird: bis Station 3 der Schlitten,
    /// an Station 4 (erst dort erreichen die Kinder den Biberdamm) der nächtliche Gang hinaus, ab Station 5 das Schloss.
    /// </summary>
    public static TemptationChapter ChapterFor(int stationNumber) => stationNumber switch
    {
        <= 3 => TemptationChapter.Sledge,
        4 => TemptationChapter.BeaverDam,
        _ => TemptationChapter.Castle,
    };

    /// <summary>
    /// Untertitel während einer Verlockung: die Königin lockt mit dem Programm, das gerade im Vordergrund ist.
    /// Ohne bekannten Programmnamen erscheint eine allgemeine Zeile.
    /// </summary>
    public static string TemptationLine(TemptationChapter chapter, string? processName)
    {
        if (!Enum.IsDefined(chapter))
        {
            throw new ArgumentOutOfRangeException(nameof(chapter));
        }

        var name = processName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return UnknownTemptation;
        }

        if (name.Length > MaxProgramNameLength)
        {
            name = string.Concat(name.AsSpan(0, MaxProgramNameLength - 1), "…");
        }

        return chapter switch
        {
            TemptationChapter.Sledge =>
                $"Ein Tropfen aus ihrer Flasche, und im Schnee liegt Türkischer Honig. Heute heißt er {name}.",
            TemptationChapter.BeaverDam =>
                $"Bei den Bibern ist es warm, und doch zieht es Edmund hinaus zu ihr. Dich lockt {name}.",
            _ => $"In ihrem Schloss gab es für Edmund nur noch trockenes Brot. Was verspricht dir {name}?",
        };
    }

    /// <summary>
    /// Frost, der in dieser Wacht noch als Frühling gilt (geplante Dauer · <see cref="RealmMoods.ThawThreshold"/>).
    /// Wer darunter bleibt, vollendet die Wacht im Frühling.
    /// </summary>
    public static TimeSpan SpringFrostAllowance(TimeSpan planned) =>
        planned <= TimeSpan.Zero ? TimeSpan.Zero : planned * RealmMoods.ThawThreshold;

    /// <summary>
    /// Frost, bis zu dem eine vollendete Wacht als Tauwetter gilt (geplante Dauer · <see cref="RealmMoods.WinterThreshold"/>).
    /// Ab dieser Menge hält der Winter Einzug.
    /// </summary>
    public static TimeSpan ThawFrostAllowance(TimeSpan planned) =>
        planned <= TimeSpan.Zero ? TimeSpan.Zero : planned * RealmMoods.WinterThreshold;

    /// <summary>
    /// Das Abschlussbild: Eine vollendete Wacht malt ihre Jahreszeit, alles andere (abgebrochen oder
    /// nicht beendet) führt zurück durch den Schrank.
    /// </summary>
    public static JourneyEnding EndingFor(SessionPhase outcome, RealmMood mood)
    {
        if (outcome != SessionPhase.Completed)
        {
            return JourneyEnding.Wardrobe;
        }

        return mood switch
        {
            RealmMood.Spring => JourneyEnding.Coronation,
            RealmMood.Thaw => JourneyEnding.Thaw,
            _ => JourneyEnding.StoneCourtyard,
        };
    }

    /// <summary>Das Abschlussbild eines Chronik-Eintrags.</summary>
    public static JourneyEnding EndingFor(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return EndingFor(record.Outcome, MoodOf(record));
    }

    /// <summary>Jahreszeit eines Chronik-Eintrags (Frost / gemessene Zeit, wie überall in der App).</summary>
    public static RealmMood MoodOf(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return RealmMoods.FromFrost(record.Measured <= TimeSpan.Zero ? 0 : record.Distracted / record.Measured);
    }

    /// <summary>
    /// Etikett, Titel und Erzählung zum Abschlussbild. Beim Abbruch bleibt die Erzählung hier allgemein;
    /// mit Stationsangabe liefert sie <see cref="Describe(SessionRecord)"/>.
    /// </summary>
    public static JourneyEndingText Describe(JourneyEnding ending) => DescribeEnding(ending, reachedStation: 0);

    /// <summary>Abschlussbild und Texte zu einem Chronik-Eintrag; beim Abbruch nennt die Erzählung die erreichte Station.</summary>
    public static JourneyEndingText Describe(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return DescribeEnding(EndingFor(record), StationNumber(record.Measured, record.Planned));
    }

    /// <summary>
    /// Erzählender Titel eines Chronik-Eintrags. Vorrang: abgebrochen („Zurück durch den Schrank · Station n“),
    /// dann ohne ein einziges Stück Honig („Die Schachtel blieb zu“), sonst das Bild der Jahreszeit.
    /// </summary>
    public static string ChronicleTitle(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (record.Outcome != SessionPhase.Completed)
        {
            return string.Create(CultureInfo.InvariantCulture,
                $"Zurück durch den Schrank · Station {StationNumber(record.Measured, record.Planned)}");
        }

        if (record.DistractionCount == 0)
        {
            return "Die Schachtel blieb zu";
        }

        return EndingFor(record) switch
        {
            JourneyEnding.Coronation => "Krönung in Cair Paravel",
            JourneyEnding.Thaw => "Der Schlitten bleibt stecken",
            _ => "Im Hof der Steinfiguren",
        };
    }

    /// <summary>
    /// Texte zum Abschlussbild. Beim Abbruch nennt die Erzählung die erreichte Station (1–10);
    /// bei 0 oder einer ungültigen Nummer bleibt sie allgemein.
    /// </summary>
    private static JourneyEndingText DescribeEnding(JourneyEnding ending, int reachedStation) => ending switch
    {
        JourneyEnding.Coronation => new(
            "FRÜHLING",
            "Krönung in Cair Paravel",
            "Kaum Frost in dieser Wacht: Der Schnee ist geschmolzen, die Bäche laufen zum Meer. Vier Throne stehen bereit – und dein Bild in der Chronik ist die Krönung."),
        JourneyEnding.Thaw => new(
            "TAUWETTER",
            "Der Schlitten bleibt stecken",
            "Der Frost blieb unter einem Viertel deiner Wacht. Wie im Buch, als ihr Schlitten im Matsch stecken blieb und Edmund die ersten Blumen sah: Der Winter der Königin verliert seine Kraft."),
        JourneyEnding.StoneCourtyard => new(
            "WINTER",
            "Noch Stein im Hof der Königin",
            "Viel Frost lag heute auf deiner Wacht, und dein Bild ist der Hof der Königin voller Steinfiguren. Doch du hast sie vollendet – und auch dieser Hof erwachte, als Aslan kam. Der goldene Schein am Tor gilt deiner nächsten Wacht."),
        JourneyEnding.Wardrobe => new(
            "ABGEBROCHEN",
            "Zurück durch den Schrank",
            WardrobeNarration(reachedStation)),
        _ => throw new ArgumentOutOfRangeException(nameof(ending)),
    };

    private static string WardrobeNarration(int reachedStation)
    {
        const string Return = "Auch Lucy kam von ihrem ersten Besuch durch den Schrank zurück – und Narnia wartete auf sie. "
            + "Die Laterne lässt sich jederzeit neu entzünden.";

        if (reachedStation is < 1 or > StationCount)
        {
            return "Deine Reise endete vor Cair Paravel. " + Return;
        }

        var station = Stations[reachedStation - 1];
        return string.Create(CultureInfo.InvariantCulture,
            $"Deine Reise endete an Station {station.Number}: {station.Name}. {Return}");
    }
}
