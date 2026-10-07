using System.Globalization;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Services;

/// <summary>
/// Erzählstimme der Anwendung. Jede Wacht ist eine Reise durch „Der König von Narnia“
/// (siehe <see cref="Journey"/>): vom Laternenpfahl bis Cair Paravel, unterwegs lockt die Königin
/// mit Türkischem Honig, und wer zurückkehrt, wird empfangen wie Edmund am Steinernen Tisch – ohne Vorwurf.
/// </summary>
/// <remarks>
/// Alle Texte sind eigene Formulierungen; nichts davon zitiert das Buch, eine Übersetzung oder einen Film.
/// Die Methoden erzeugen nur dann neue Zeichenketten, wenn ein Wert eingesetzt werden muss –
/// feste Sätze sind Literale, damit der Sekundentakt sparsam bleibt.
/// </remarks>
internal static class Lore
{
    /// <summary>Wie lange eine Rückkehr als „Zurück im Licht“ gefeiert wird.</summary>
    public static readonly TimeSpan ReturnWelcomeDuration = TimeSpan.FromSeconds(20);

    /// <summary>Ab dieser Länge einer Verlockung wechselt die Bildzeile zur leiseren Fassung.</summary>
    public static readonly TimeSpan LongTemptation = TimeSpan.FromMinutes(10);

    /// <summary>Weniger Rest bis zur nächsten Jahreszeit färbt die Zeile des Wintermessers rot.</summary>
    public static readonly TimeSpan WinterMeterWarning = TimeSpan.FromMinutes(1);

    // ===== Bereit =====
    public const string ReadyStatus = "Der Schrank steht offen";
    public const string ReadySubtitle = "Hinter den Pelzmänteln liegt ein Wald, in dem der Winter nicht enden will.";

    // ===== Unterwegs =====
    public const string LightStatus = "Die Laterne brennt hell";
    public const string LightSubtitleStart = "Der Pfad ist frei, im Wald ist es still.";
    public const string ReturnStatus = "Zurück im Licht";
    public const string ReturnArchLabel = "WIE AM STEINERNEN TISCH";
    public const string ReturnArchText = "Wer zurückkommt, wird hier nicht ausgefragt. So war es bei Edmund, so ist es bei dir.";
    public const string RestStatus = "Rast bei den Bibern";
    public const string RestSubtitle = "Frau Biber hat den Kessel aufgesetzt. Solange du rastest, steht die Reise still.";
    public const string RestRoute = "Die Reise steht still, solange du rastest.";
    public const string AwayStatus = "Die Laterne wacht allein";
    public const string TemptationStatus = "Der Schlitten der Königin hält";
    public const string TemptationArchLabel = "ANDERS ALS EDMUND";
    public const string TemptationArchText = "Edmund blieb auf dem Schlitten sitzen. Du kannst jederzeit aufstehen – die Spur zur Laterne ist noch zu sehen.";
    public const string TemptationArchTextLong = "Die Spur zur Laterne verweht nicht. Ein Wechsel zu deinen Gefährten genügt.";
    public const string WinterLine = "Der Winter hat diese Wacht erreicht. Auch Edmund fand von dort zurück – bring sie zu Ende, Cair Paravel wartet.";

    // ===== Ende =====
    public const string EyebrowCompleted = "KAPITEL I · DIE WACHT IST VOLLENDET";
    public const string EyebrowAborted = "KAPITEL I · DIE WACHT RUHT";
    public const string EyebrowReady = "KAPITEL I";

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private static readonly string[] Proverbs =
    [
        "Ein Licht im Schnee genügt, um den Weg nach Hause zu finden.",
        "Der Winter endet nicht an einem Tag – aber er endet.",
        "Wer die Laterne hütet, hütet den Pfad.",
        "Nicht jede Tür im Schrank führt dorthin, wo du hinwillst.",
        "Der Löwe brüllt nicht für die Säumigen, sondern für die Wachen.",
        "Süßes vom Schlitten macht nur hungriger.",
        "Auch die längste Nacht im Wald kennt einen Morgen.",
        "Wer umkehrt, ist nicht verloren, sondern auf dem Heimweg.",
    ];

    public static string Proverb(int seed) => Proverbs[Math.Abs(seed % Proverbs.Length)];

    /// <summary>
    /// Statuszeile (Pille im Bild) und Untertitel der Seite „Die Wacht“.
    /// </summary>
    /// <param name="session">Die gezeigte Wacht oder <c>null</c>, wenn der Schrank offen steht.</param>
    /// <param name="snapshot">Letzte Messung (für die Leerlaufzeit bei Abwesenheit).</param>
    /// <param name="returnedFrost">Dauer der eben beendeten Verlockung, solange die Rückkehr gefeiert wird; sonst <c>null</c>.</param>
    public static (string Headline, string Detail) Describe(FocusSession? session, ActivitySnapshot? snapshot, TimeSpan? returnedFrost = null)
    {
        if (session is null)
        {
            return (ReadyStatus, ReadySubtitle);
        }

        return session.Phase switch
        {
            SessionPhase.Paused => (RestStatus, RestSubtitle),
            SessionPhase.Aborted or SessionPhase.Completed => (EndingTitle(session), EndingSubtitle(session.Measured, session.Distracted, session.DistractionCount)),
            _ => session.CurrentState switch
            {
                ActivityState.Distracted => (TemptationStatus,
                    Journey.TemptationLine(Journey.ChapterFor(Journey.StationNumber(session.Measured, session.Planned)), session.CurrentProcess)),
                ActivityState.Away => (AwayStatus,
                    $"Keine Spur im Schnee seit {Span(snapshot?.IdleTime ?? TimeSpan.Zero)}. Das zählt nicht als Frost – die Laterne wartet auf dich."),
                _ when returnedFrost is { } frost => (ReturnStatus,
                    $"Der Schlitten fährt ohne dich davon. {Span(frost)} Frost bleiben im Schnee zurück, die Reise geht weiter."),
                _ => (LightStatus, session.CurrentStreak >= TimeSpan.FromMinutes(1)
                    ? $"Seit {TimeFormat.Clock(session.CurrentStreak)} im Licht – die Glöckchen der Königin bleiben fern."
                    : LightSubtitleStart),
            },
        };
    }

    /// <summary>Kurzer Status für die Fokusleiste, z. B. "Im Licht · Serie 12:30" oder "Am Schlitten · Hearthstone".</summary>
    public static string Short(FocusSession? session)
    {
        if (session is null)
        {
            return ReadyStatus;
        }

        return session.Phase switch
        {
            SessionPhase.Paused => "Rast bei den Bibern · die Reise steht still",
            SessionPhase.Aborted or SessionPhase.Completed => EndingTitle(session),
            _ => session.CurrentState switch
            {
                ActivityState.Distracted => session.CurrentProcess is { Length: > 0 } p ? "Am Schlitten · " + p : "Am Schlitten",
                ActivityState.Away => "Die Laterne wartet",
                _ => session.CurrentStreak >= TimeSpan.FromMinutes(1)
                    ? "Im Licht · Serie " + TimeFormat.Clock(session.CurrentStreak)
                    : "Im Licht",
            },
        };
    }

    /// <summary>Titel und Erzählung des Abschlussbildes einer vollendeten Wacht in dieser Jahreszeit.</summary>
    public static (string Headline, string Detail) Completed(RealmMood mood)
    {
        var text = Journey.Describe(Journey.EndingFor(SessionPhase.Completed, mood));
        return (text.Title, text.Narration);
    }

    public static string MoodName(RealmMood mood) => mood switch
    {
        RealmMood.Spring => "Frühling",
        RealmMood.Thaw => "Tauwetter",
        _ => "Winter",
    };

    public static string OutcomeName(SessionPhase phase) => phase switch
    {
        SessionPhase.Completed => "Vollendet",
        SessionPhase.Aborted => "Abgebrochen",
        _ => "Unterwegs",
    };

    // ===== Bausteine der Reise =====

    /// <summary>Kurze Zeitangabe wie im Erzähltext: "2:02", ab einer Stunde "1:02:03" (Sekundenbruchteile abgeschnitten).</summary>
    public static string Span(TimeSpan value)
    {
        var totalSeconds = value <= TimeSpan.Zero ? 0 : (long)value.TotalSeconds;
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? string.Create(Invariant, $"{hours}:{minutes:00}:{seconds:00}")
            : string.Create(Invariant, $"{minutes}:{seconds:00}");
    }

    /// <summary>Wie <see cref="Span"/>, aber auf ganze Sekunden aufgerundet – für Countdowns, die nie zu früh "0:00" zeigen.</summary>
    public static string SpanUp(TimeSpan value) =>
        Span(value <= TimeSpan.Zero ? TimeSpan.Zero : TimeSpan.FromSeconds(Math.Ceiling(value.TotalSeconds)));

    /// <summary>"KAPITEL I · STATION 3 VON 10" (Augenbraue der laufenden Wacht).</summary>
    public static string Eyebrow(int station) => string.Create(Invariant, $"KAPITEL I · STATION {station} VON 10");

    /// <summary>"STATION 3 VON 10 · DIE LEERE HÖHLE" (Zeile über der Stationserzählung im Bild).</summary>
    public static string StationLabel(JourneyStation station) =>
        string.Create(Invariant, $"STATION {station.Number} VON 10 · {station.Name.ToUpper(CultureInfo.GetCultureInfo("de-DE"))}");

    /// <summary>"STATION 6 · DAS TAUWETTER" (Zeile über der Pille im Abschlussbild).</summary>
    public static string EndingStationLabel(JourneyStation station) =>
        string.Create(Invariant, $"STATION {station.Number} · {station.Name.ToUpper(CultureInfo.GetCultureInfo("de-DE"))}");

    /// <summary>Zeile unter dem Wegband: nächste Station mit Restzeit, an Station 9 die letzte Etappe.</summary>
    public static string NextStation(int station, TimeSpan untilNext)
    {
        if (station >= Journey.StationCount)
        {
            return "Cair Paravel ist erreicht.";
        }

        return station == Journey.StationCount - 1
            ? "Letzte Etappe: Cair Paravel · in " + SpanUp(untilNext)
            : "Nächste Station: " + Journey.Stations[station].Name + " · in " + SpanUp(untilNext);
    }

    /// <summary>Kurzbeschreibung des Wegbands für Screenreader (ohne Sekunden, damit sie nicht jede Sekunde neu entsteht).</summary>
    public static string JourneyDescription(int station, int honey)
    {
        var current = Journey.Stations[Math.Clamp(station, 1, Journey.StationCount) - 1];
        var next = station < Journey.StationCount ? " Nächste Station: " + Journey.Stations[station].Name + "." : string.Empty;
        return string.Create(Invariant, $"Reise: Station {current.Number} von 10, {current.Name}. Bisher {honey} Stück Türkischer Honig.{next}");
    }

    /// <summary>"3 Stück" – Türkischer Honig aus der Schachtel der Königin.</summary>
    public static string Honey(int count) => string.Create(Invariant, $"{count} Stück");

    /// <summary>
    /// Zeile unter dem Wintermesser: wie viel Frost noch bleibt, bis die Jahreszeit kippt.
    /// <paramref name="isWarning"/> ist gesetzt, wenn die nächste Grenze näher als eine Minute ist.
    /// </summary>
    public static string WinterMeterLine(TimeSpan frost, TimeSpan planned, out bool isWarning)
    {
        var spring = Journey.SpringFrostAllowance(planned);
        var thaw = Journey.ThawFrostAllowance(planned);
        if (frost < spring)
        {
            isWarning = spring - frost < WinterMeterWarning;
            return "Noch " + SpanUp(spring - frost) + " Frost, dann wird aus Frühling Tauwetter.";
        }

        if (frost < thaw)
        {
            isWarning = thaw - frost < WinterMeterWarning;
            return "Tauwetter. Noch " + SpanUp(thaw - frost) + " Frost, dann hält der Winter Einzug.";
        }

        isWarning = false;
        return WinterLine;
    }

    /// <summary>Beschreibung des Wintermessers für Screenreader.</summary>
    public static string WinterMeterDescription(TimeSpan frost, TimeSpan planned) =>
        "Frost " + Span(frost) + " von " + TimeFormat.Clock(planned) + ". Frühling bis " + Span(Journey.SpringFrostAllowance(planned))
        + ", Tauwetter bis " + Span(Journey.ThawFrostAllowance(planned)) + ".";

    /// <summary>"Frühling bis 2:30 Frost · Tauwetter bis 6:15" (Frostvorrat vor dem Aufbruch).</summary>
    public static string FrostAllowance(TimeSpan planned) =>
        "Frühling bis " + Span(Journey.SpringFrostAllowance(planned)) + " Frost · Tauwetter bis " + Span(Journey.ThawFrostAllowance(planned));

    /// <summary>"Vom Laternenpfahl bis Cair Paravel · 25:00".</summary>
    public static string ReadyRoute(TimeSpan planned) => "Vom Laternenpfahl bis Cair Paravel · " + TimeFormat.Clock(planned);

    /// <summary>"von 25:00 Minuten" unter der Restzeit (ab einer Stunde ohne "Minuten").</summary>
    public static string PlannedLine(TimeSpan planned) =>
        planned >= TimeSpan.FromHours(1) ? "von " + TimeFormat.Clock(planned) : "von " + TimeFormat.Clock(planned) + " Minuten";

    /// <summary>Titel des Abschlussbildes einer beendeten Wacht.</summary>
    public static string EndingTitle(FocusSession session) =>
        Journey.Describe(Journey.EndingFor(session.Phase, RealmMoods.FromFrost(session.FrostRatio))).Title;

    /// <summary>"25:00 gewacht · 1:48 Frost · 2 Stück Türkischer Honig".</summary>
    public static string EndingSubtitle(TimeSpan measured, TimeSpan frost, int honey) =>
        string.Create(Invariant, $"{Span(measured)} gewacht · {Span(frost)} Frost · {honey} Stück Türkischer Honig");

    /// <summary>Pille im Abschlussbild: "Frühling · 7 % Frost" bzw. "Abgebrochen · Station 6".</summary>
    public static string EndingPill(SessionRecord record, int station)
    {
        if (record.Outcome != SessionPhase.Completed)
        {
            return string.Create(Invariant, $"Abgebrochen · Station {station}");
        }

        // Abgerundet, damit die Zahl nie über der Grenze der genannten Jahreszeit liegt (9,6 % bleibt Frühling = 9 %).
        var mood = Journey.MoodOf(record);
        var ratio = record.Measured <= TimeSpan.Zero ? 0 : record.Distracted / record.Measured;
        var percent = (int)Math.Floor(Math.Clamp(ratio, 0, 1) * 100);
        return string.Create(Invariant, $"{MoodName(mood)} · {percent} % Frost");
    }

    /// <summary>Zusatz neben "Die Reise" im Abschluss: "vollendet · 25:00" bzw. "9:20 von 15:00".</summary>
    public static string EndingCaption(SessionRecord record) =>
        record.Outcome == SessionPhase.Completed
            ? "vollendet · " + TimeFormat.Clock(record.Planned)
            : Span(record.Measured) + " von " + Span(record.Planned);

    /// <summary>Zeile unter dem Wegband im Abschluss.</summary>
    public static string EndingRoute(SessionRecord record) =>
        record.Outcome == SessionPhase.Completed
            ? "Alle zehn Stationen"
            : "Bis Cair Paravel fehlten " + SpanUp(record.Planned - record.Measured);

    /// <summary>Schriftband bei neuer Bestleistung.</summary>
    public static string RecordStreak(TimeSpan streak) =>
        "Neue Bestmarke: " + TimeFormat.Clock(streak) + " am Stück im Licht. So lange hast du die Laterne noch nie gehalten.";
}
