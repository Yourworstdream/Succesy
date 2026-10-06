using System.Collections.ObjectModel;
using System.Globalization;
using Laternenwacht.App.Services;
using Laternenwacht.Platform.Windows;
using Laternenwacht.Core.Integrity;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.ViewModels;

/// <summary>Die Chronik: versiegelte Historie aller Wachten und der Zustand des Siegels.</summary>
internal sealed class ChronicleViewModel : ObservableObject
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    private readonly SessionJournal? _journal;
    private SealStatus _sealStatus;
    private string _sealMessage;
    private string? _archiveHint;

    public ChronicleViewModel(JournalOpenResult? opened, string? openError)
    {
        _journal = opened?.Journal;

        if (opened is null)
        {
            _sealStatus = SealStatus.Broken;
            _sealMessage = "Die Chronik konnte nicht geöffnet werden: " + openError;
        }
        else
        {
            _sealStatus = opened.Verification.Status;
            _sealMessage = opened.Verification.Message;
            if (opened.ArchivedTo is not null)
            {
                _archiveHint = $"Die gebrochene Chronik wurde unverändert archiviert unter: {opened.ArchivedTo}. Eine neue, versiegelte Chronik wurde begonnen.";
            }

            foreach (var record in opened.Journal.Records.Reverse())
            {
                Entries.Add(new ChronicleEntry(record));
            }
        }

        UpdateTotals();
    }

    public ObservableCollection<ChronicleEntry> Entries { get; } = [];

    public SealStatus SealStatus { get => _sealStatus; private set => SetProperty(ref _sealStatus, value); }

    public bool IsSealIntact => SealStatus != SealStatus.Broken;

    public string SealHeadline => SealStatus switch
    {
        SealStatus.Intact => "Das Siegel ist unversehrt",
        SealStatus.Empty => "Ein frisches Pergament",
        _ => "Das Siegel ist gebrochen!",
    };

    public string SealMessage { get => _sealMessage; private set => SetProperty(ref _sealMessage, value); }

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

    public string TotalFocusedText { get; private set; } = "0:00:00";

    public string TotalFrostText { get; private set; } = "0:00:00";

    public int CompletedCount { get; private set; }

    public string SpringShareText { get; private set; } = "–";

    /// <summary>Bisher längste ununterbrochene Fokusphase über alle Wachten.</summary>
    public TimeSpan BestStreak => Entries.Count == 0 ? TimeSpan.Zero : Entries.Max(e => e.Record.LongestFocusStreak);

    public string BestStreakText => BestStreak > TimeSpan.Zero ? TimeFormat.Clock(BestStreak) : "–";

    /// <summary>Trägt eine beendete Wacht versiegelt in die Chronik ein.</summary>
    public void Record(SessionRecord record)
    {
        Entries.Insert(0, new ChronicleEntry(record));

        if (_journal is null)
        {
            SealMessage = "Die Wacht wird angezeigt, konnte aber nicht dauerhaft gespeichert werden.";
            UpdateTotals();
            return;
        }

        try
        {
            _journal.Append(record);
            var verification = _journal.Verify();
            SealStatus = verification.Status;
            SealMessage = verification.Message;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            AppLog.Error("Chronik", ex);
            SealStatus = SealStatus.Broken;
            SealMessage = "Die Wacht konnte nicht in die Chronik geschrieben werden: " + ex.Message;
        }

        UpdateTotals();
    }

    private void UpdateTotals()
    {
        var focused = TimeSpan.FromTicks(Entries.Sum(e => e.Record.Focused.Ticks));
        var frost = TimeSpan.FromTicks(Entries.Sum(e => e.Record.Distracted.Ticks));
        TotalFocusedText = TimeFormat.Clock(focused);
        TotalFrostText = TimeFormat.Clock(frost);
        CompletedCount = Entries.Count(e => e.Record.Outcome == SessionPhase.Completed);
        SpringShareText = Entries.Count == 0
            ? "–"
            : ((double)Entries.Count(e => e.Mood == RealmMood.Spring) / Entries.Count).ToString("P0", German);

        OnPropertyChanged(nameof(TotalFocusedText));
        OnPropertyChanged(nameof(TotalFrostText));
        OnPropertyChanged(nameof(CompletedCount));
        OnPropertyChanged(nameof(SpringShareText));
        OnPropertyChanged(nameof(BestStreakText));
        OnPropertyChanged(nameof(IsSealIntact));
        OnPropertyChanged(nameof(SealHeadline));
        OnPropertyChanged(nameof(SealShort));
        OnPropertyChanged(nameof(SealDetailShort));
        OnPropertyChanged(nameof(HasArchiveHint));
    }
}

/// <summary>Eine Zeile der Chronik, aufbereitet für die Anzeige.</summary>
internal sealed class ChronicleEntry(SessionRecord record)
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    public SessionRecord Record { get; } = record;

    public RealmMood Mood { get; } = RealmMoods.FromFrost(record.Measured <= TimeSpan.Zero ? 0 : record.Distracted / record.Measured);

    public string DateText => Record.StartedAtUtc.ToLocalTime().ToString("ddd, dd.MM.yyyy · HH:mm", German);

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
}
