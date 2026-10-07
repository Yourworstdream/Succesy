using System.Collections.ObjectModel;
using System.Globalization;
using Laternenwacht.App.Services;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Integrity;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.ViewModels;

/// <summary>
/// Die Chronik: versiegelte Historie aller Wachten, der Zustand des Siegels und die Summen
/// („Die Bilder der Chronik“: Krönungen, Tauwetter, Steinhof).
/// </summary>
/// <remarks>
/// Alles, was eine Zeile anzeigt (Bildtitel, erreichte Station, Honig, Jahreszeit), wird genau einmal je Eintrag
/// berechnet – beim Öffnen bzw. beim Eintragen einer neuen Wacht. Die Summen werden nur neu gebildet, wenn eine Wacht
/// hinzukommt (einmal je Wacht, nie im Sekundentakt).
/// </remarks>
internal sealed class ChronicleViewModel : ObservableObject
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly SessionJournal? _journal;
    private SealStatus _sealStatus;
    private string _sealMessage;
    private string? _archiveHint;
    private TimeSpan _bestStreak;

    public ChronicleViewModel(JournalOpenResult? opened, string? openError)
    {
        _journal = opened?.Journal;

        if (opened is null)
        {
            _sealStatus = SealStatus.Broken;
            _sealMessage = "Die Chronik konnte nicht geöffnet werden: " + openError;
            Entries = [];
        }
        else
        {
            _sealStatus = opened.Verification.Status;
            _sealMessage = opened.Verification.Message;
            if (opened.ArchivedTo is not null)
            {
                _archiveHint = $"Die gebrochene Chronik wurde unverändert archiviert unter: {opened.ArchivedTo}. Eine neue, versiegelte Chronik wurde begonnen.";
            }

            // Chronologisch durchgehen, damit jede Wacht ihre Bestmarke gegen die früheren messen kann,
            // danach neueste zuerst anzeigen.
            var records = opened.Journal.Records;
            var sealedByJournal = _sealStatus != SealStatus.Broken;
            var entries = new ChronicleEntry[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                entries[records.Count - 1 - i] = CreateEntry(records[i], sealedByJournal);
            }

            Entries = new ObservableCollection<ChronicleEntry>(entries);
        }

        UpdateTotals();
    }

    /// <summary>Alle Wachten, neueste zuerst.</summary>
    public ObservableCollection<ChronicleEntry> Entries { get; }

    public SealStatus SealStatus { get => _sealStatus; private set => SetProperty(ref _sealStatus, value); }

    public bool IsSealIntact => SealStatus != SealStatus.Broken;

    public string SealHeadline => SealStatus switch
    {
        SealStatus.Intact => "Das Siegel ist unversehrt",
        SealStatus.Empty => "Ein frisches Pergament",
        _ => "Das Siegel ist gebrochen!",
    };

    public string SealMessage { get => _sealMessage; private set => SetProperty(ref _sealMessage, value); }

    /// <summary>
    /// Text der Siegelkarte: bei unversehrtem Siegel die Zusage, dass alle Wachten lückenlos verkettet sind;
    /// sonst die Meldung der Prüfung (frisches Pergament, gebrochenes Siegel, Schreibfehler).
    /// </summary>
    public string SealText => SealStatus != SealStatus.Intact
        ? SealMessage
        : Entries.Count switch
        {
            0 => SealMessage,
            1 => "Die erste Wacht ist versiegelt – keine Seite wurde heimlich neu geschrieben.",
            var n => string.Create(German, $"Alle {n} Wachten sind lückenlos verkettet – keine Seite wurde heimlich neu geschrieben."),
        };

    /// <summary>Kurzform für die Navigationsleiste.</summary>
    public string SealShort => SealStatus switch
    {
        SealStatus.Intact => "Siegel intakt",
        SealStatus.Empty => "Chronik bereit",
        _ => "Siegel gebrochen",
    };

    public string SealDetailShort => SealStatus switch
    {
        SealStatus.Broken => "Details in der Chronik",
        _ => Entries.Count == 1 ? "1 Wacht geprüft" : $"{Entries.Count} Wachten geprüft",
    };

    public string? ArchiveHint { get => _archiveHint; private set => SetProperty(ref _archiveHint, value); }

    public bool HasArchiveHint => !string.IsNullOrEmpty(ArchiveHint);

    /// <summary><c>true</c>, solange noch keine Wacht verzeichnet ist (Leerzustand des Verzeichnisses).</summary>
    public bool IsEmpty => Entries.Count == 0;

    /// <summary>Summe der Fokuszeit, z. B. „9:42 h“.</summary>
    public string TotalFocusedText { get; private set; } = "0:00 h";

    /// <summary>Summe der Ablenkungszeit, z. B. „0:51 h“.</summary>
    public string TotalFrostText { get; private set; } = "0:00 h";

    public int CompletedCount { get; private set; }

    /// <summary>Anteil aller Wachten, die im Frühling endeten (bisherige Kennzahl, bleibt erhalten).</summary>
    public string SpringShareText { get; private set; } = "–";

    /// <summary>KRONEN: vollendete Wachten im Frühling – jede endete mit der Krönung in Cair Paravel.</summary>
    public int CrownCount => Coronations.Count;

    /// <summary>Die Bilder der Chronik: Krönungen (vollendet im Frühling).</summary>
    public ChroniclePictureStat Coronations { get; } = new();

    /// <summary>Die Bilder der Chronik: Tauwetter (vollendet, der Schlitten bleibt stecken).</summary>
    public ChroniclePictureStat Thaws { get; } = new();

    /// <summary>Die Bilder der Chronik: Steinhof (vollendet im Winter).</summary>
    public ChroniclePictureStat StoneCourtyards { get; } = new();

    /// <summary>Bisher längste ununterbrochene Fokusphase über alle Wachten.</summary>
    public TimeSpan BestStreak => _bestStreak;

    public string BestStreakText => BestStreak > TimeSpan.Zero ? TimeFormat.Clock(BestStreak) : "–";

    /// <summary>Trägt eine beendete Wacht versiegelt in die Chronik ein.</summary>
    public void Record(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        var isSealed = false;
        if (_journal is null)
        {
            SealMessage = "Die Wacht wird angezeigt, konnte aber nicht dauerhaft gespeichert werden.";
        }
        else
        {
            try
            {
                _journal.Append(record);
                var verification = _journal.Verify();
                SealStatus = verification.Status;
                SealMessage = verification.Message;
                isSealed = verification.Status != SealStatus.Broken;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                AppLog.Error("Chronik", ex);
                SealStatus = SealStatus.Broken;
                SealMessage = "Die Wacht konnte nicht in die Chronik geschrieben werden: " + ex.Message;
            }
        }

        Entries.Insert(0, CreateEntry(record, isSealed));
        UpdateTotals();
    }

    /// <summary>Baut die Zeile einer Wacht; die Bestmarke zählt nur gegen frühere Einträge mit gemessener Serie.</summary>
    private ChronicleEntry CreateEntry(SessionRecord record, bool isSealed)
    {
        var entry = new ChronicleEntry(record, _bestStreak, isSealed);
        if (record.LongestFocusStreak > _bestStreak)
        {
            _bestStreak = record.LongestFocusStreak;
        }

        return entry;
    }

    private void UpdateTotals()
    {
        long focusedTicks = 0, frostTicks = 0;
        int completed = 0, spring = 0, coronations = 0, thaws = 0, courtyards = 0;
        foreach (var entry in Entries)
        {
            focusedTicks += entry.Record.Focused.Ticks;
            frostTicks += entry.Record.Distracted.Ticks;
            if (entry.Mood == RealmMood.Spring)
            {
                spring++;
            }

            if (!entry.IsCompleted)
            {
                continue;
            }

            completed++;
            switch (entry.Ending)
            {
                case JourneyEnding.Coronation:
                    coronations++;
                    break;
                case JourneyEnding.Thaw:
                    thaws++;
                    break;
                case JourneyEnding.StoneCourtyard:
                    courtyards++;
                    break;
            }
        }

        TotalFocusedText = Hours(TimeSpan.FromTicks(focusedTicks));
        TotalFrostText = Hours(TimeSpan.FromTicks(frostTicks));
        CompletedCount = completed;
        SpringShareText = Entries.Count == 0 ? "–" : ((double)spring / Entries.Count).ToString("P0", German);
        Coronations.Update(coronations, completed);
        Thaws.Update(thaws, completed);
        StoneCourtyards.Update(courtyards, completed);

        OnPropertyChanged(nameof(TotalFocusedText));
        OnPropertyChanged(nameof(TotalFrostText));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(SpringShareText));
        OnPropertyChanged(nameof(CrownCount));
        OnPropertyChanged(nameof(BestStreak));
        OnPropertyChanged(nameof(BestStreakText));
        OnPropertyChanged(nameof(IsSealIntact));
        OnPropertyChanged(nameof(SealHeadline));
        OnPropertyChanged(nameof(SealText));
        OnPropertyChanged(nameof(SealShort));
        OnPropertyChanged(nameof(SealDetailShort));
        OnPropertyChanged(nameof(HasArchiveHint));
        OnPropertyChanged(nameof(IsEmpty));
    }

    /// <summary>Stunden und Minuten mit „h“, z. B. „9:42 h“ (Summen über viele Wachten).</summary>
    private static string Hours(TimeSpan value)
    {
        var totalMinutes = (long)Math.Max(0, value.TotalMinutes);
        return string.Create(CultureInfo.InvariantCulture, $"{totalMinutes / 60}:{totalMinutes % 60:00} h");
    }
}

/// <summary>Eine Zeile in „Die Bilder der Chronik“: Anzahl eines Abschlussbildes und sein Anteil an den vollendeten Wachten.</summary>
internal sealed class ChroniclePictureStat : ObservableObject
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private int _count;
    private double _share;
    private string _shareText = "–";

    public int Count { get => _count; private set => SetProperty(ref _count, value); }

    /// <summary>Anteil an den vollendeten Wachten (0..1) – Breite des Balkens.</summary>
    public double Share
    {
        get => _share;
        private set
        {
            if (SetProperty(ref _share, value))
            {
                OnPropertyChanged(nameof(Rest));
            }
        }
    }

    /// <summary>Der Rest des Balkens (1 − <see cref="Share"/>), für das Sternmaß der Spalten.</summary>
    public double Rest => 1 - Share;

    /// <summary>Anteil als Text, z. B. „71 %“; „–“ ohne vollendete Wachten.</summary>
    public string ShareText { get => _shareText; private set => SetProperty(ref _shareText, value); }

    public void Update(int count, int completed)
    {
        Count = count;
        Share = completed <= 0 ? 0 : Math.Clamp((double)count / completed, 0, 1);
        ShareText = completed <= 0 ? "–" : Share.ToString("P0", German);
    }
}

/// <summary>Eine Zeile der Chronik, aufbereitet für die Anzeige. Alles Erzählerische wird einmal beim Anlegen berechnet.</summary>
internal sealed class ChronicleEntry
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <param name="record">Die versiegelte Wacht.</param>
    /// <param name="bestBefore">Längste Fokus-Serie aller früheren Wachten (für „Neue Bestmarke“).</param>
    /// <param name="isSealed"><c>false</c>, wenn die Wacht nicht (gültig) in die Chronik geschrieben werden konnte.</param>
    public ChronicleEntry(SessionRecord record, TimeSpan bestBefore, bool isSealed = true)
    {
        ArgumentNullException.ThrowIfNull(record);

        Record = record;
        IsSealed = isSealed;
        IsCompleted = record.Outcome == SessionPhase.Completed;
        Mood = Journey.MoodOf(record);
        Ending = Journey.EndingFor(record);
        StationReached = IsCompleted ? Journey.StationCount : Journey.StationNumber(record.Measured, record.Planned);
        JourneyPosition = Journey.PositionOf(StationReached);

        // Vorrang der Bildtitel: Abgebrochen > Neue Bestmarke > Die Schachtel blieb zu > Jahreszeit.
        // Eine Bestmarke braucht frühere Einträge mit gemessener Serie – sonst würde die erste Wacht nach dem Update gefeiert.
        IsNewRecord = IsCompleted && bestBefore > TimeSpan.Zero && record.LongestFocusStreak > bestBefore;
        StoryTitle = IsNewRecord
            ? "Neue Bestmarke · " + TimeFormat.Clock(record.LongestFocusStreak)
            : Journey.ChronicleTitle(record);

        var planned = PlannedText;
        JourneyText = IsCompleted
            ? planned + " · bis Cair Paravel"
            : string.Create(German, $"{TimeFormat.Clock(record.Measured)} von {planned} · Station {StationReached}");
        JourneyDescription = IsCompleted
            ? "Reise vollendet: alle 10 Stationen bis Cair Paravel"
            : string.Create(German, $"Reise abgebrochen an Station {StationReached} von 10: {Journey.Stations[StationReached - 1].Name}");
    }

    public SessionRecord Record { get; }

    /// <summary>Jahreszeit der Wacht (Frost / gemessene Zeit), wie in der Chronik seit jeher.</summary>
    public RealmMood Mood { get; }

    /// <summary>Abschlussbild: Krönung, Tauwetter, Steinhof oder zurück durch den Schrank.</summary>
    public JourneyEnding Ending { get; }

    public bool IsCompleted { get; }

    /// <summary>Abgebrochene Wachten erscheinen im Verzeichnis gedämpft.</summary>
    public bool IsAborted => !IsCompleted;

    /// <summary><c>false</c>, wenn diese Wacht nicht gültig versiegelt werden konnte.</summary>
    public bool IsSealed { get; }

    /// <summary>Diese Wacht übertraf die längste Fokus-Serie aller früheren Wachten.</summary>
    public bool IsNewRecord { get; }

    /// <summary>Bildtitel der Wacht, z. B. „Krönung in Cair Paravel“ oder „Zurück durch den Schrank · Station 6“.</summary>
    public string StoryTitle { get; }

    /// <summary>Erreichte Station (1–10); eine vollendete Wacht erreicht immer Cair Paravel.</summary>
    public int StationReached { get; }

    /// <summary>Lage der erreichten Station auf dem Wegband (0..1).</summary>
    public double JourneyPosition { get; }

    /// <summary>Zeile unter dem kleinen Wegband, z. B. „25 min · bis Cair Paravel“ oder „9:20 von 15 min · Station 6“.</summary>
    public string JourneyText { get; }

    /// <summary>Beschreibung des Wegbands für Bildschirmleser und Tooltip.</summary>
    public string JourneyDescription { get; }

    /// <summary>Stücke Türkischer Honig = Ablenkungs-Episoden.</summary>
    public int HoneyCount => Record.DistractionCount;

    public bool HasHoney => Record.DistractionCount > 0;

    public string HoneyText => Record.DistractionCount > 0 ? Record.DistractionCount.ToString(German) : "—";

    public string HoneyDescription => Record.DistractionCount switch
    {
        0 => "Kein Stück Türkischer Honig – die Schachtel blieb zu.",
        1 => "1 Stück Türkischer Honig (eine Ablenkung)",
        var n => string.Create(German, $"{n} Stück Türkischer Honig ({n} Ablenkungen)"),
    };

    public string DateText => Record.StartedAtUtc.ToLocalTime().ToString("ddd, d. MMM yyyy · HH:mm", German);

    public string PlannedText => $"{Record.Planned.TotalMinutes:0} min";

    public string FocusedText => TimeFormat.Clock(Record.Focused);

    public string FrostText => TimeFormat.Clock(Record.Distracted);

    public string LongestStreakText => Record.LongestFocusStreak > TimeSpan.Zero ? TimeFormat.Clock(Record.LongestFocusStreak) : "–";

    public string DistractionsText => Record.DistractionCount == 1 ? "1 Verlockung" : $"{Record.DistractionCount} Verlockungen";

    public string OutcomeText => Lore.OutcomeName(Record.Outcome);

    public string MoodText => Lore.MoodName(Mood);

    public string TopDistractionText => Record.TopDistractions.Count == 0
        ? "Keine Verlockung hielt dich auf."
        : "Größte Verlockung: " + Record.TopDistractions[0].ProcessName + " (" + TimeFormat.Clock(Record.TopDistractions[0].Duration) + ")";

    /// <summary>Einzelheiten für den Tooltip der Zeile (erst beim Zeigen gebildet).</summary>
    public string DetailText =>
        $"{OutcomeText} · {MoodText} · geplant {PlannedText}\nIm Licht {FocusedText} · im Frost {FrostText} · längste Serie {LongestStreakText}\n{TopDistractionText}";
}
