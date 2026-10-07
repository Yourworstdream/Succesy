using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>
/// Zustandsautomat einer einzelnen Fokussitzung ("Wacht").
/// </summary>
/// <remarks>
/// Zeiten werden über einen monotonen Zeitstempel (<see cref="TimeProvider.GetTimestamp"/>) gemessen.
/// Ein Verstellen der Systemuhr verfälscht die Messung daher nicht.
/// Die Zeit zwischen zwei Messungen wird dem zuvor gemessenen Zustand zugerechnet.
/// </remarks>
public sealed class FocusSession
{
    /// <summary>
    /// Größere Lücken zwischen zwei Messungen (z. B. Standby) werden als Abwesenheit gewertet,
    /// weil über diese Zeit keine Aussage möglich ist.
    /// </summary>
    public static readonly TimeSpan MaxTickGap = TimeSpan.FromSeconds(30);

    private readonly TimeProvider _time;
    private readonly Dictionary<string, TimeSpan> _distractionsByProcess = new(StringComparer.Ordinal);
    private readonly List<TimeSpan> _distractionStarts = [];
    private long _lastTimestamp;

    public FocusSession(TimeSpan planned, TimeProvider time, ActivityState initialState, string? initialProcess)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(planned, TimeSpan.Zero);
        ArgumentNullException.ThrowIfNull(time);

        _time = time;
        Id = Guid.NewGuid();
        Planned = planned;
        StartedAtUtc = time.GetUtcNow();
        _lastTimestamp = time.GetTimestamp();
        Phase = SessionPhase.Running;
        CurrentState = initialState;
        CurrentProcess = initialProcess;
        DistractionStarts = _distractionStarts.AsReadOnly();
        if (initialState == ActivityState.Distracted)
        {
            DistractionCount = 1;
            _distractionStarts.Add(TimeSpan.Zero);
        }
    }

    public Guid Id { get; }

    public TimeSpan Planned { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? EndedAtUtc { get; private set; }

    public SessionPhase Phase { get; private set; }

    public ActivityState CurrentState { get; private set; }

    public string? CurrentProcess { get; private set; }

    public TimeSpan Focused { get; private set; }

    public TimeSpan Distracted { get; private set; }

    public TimeSpan Away { get; private set; }

    /// <summary>Anzahl der Wechsel in den Zustand "abgelenkt".</summary>
    public int DistractionCount { get; private set; }

    /// <summary>
    /// Gemessene Zeit (<see cref="Measured"/>) zu Beginn jeder Ablenkungs-Episode, in zeitlicher Reihenfolge –
    /// je Episode genau ein Eintrag (also stets <see cref="DistractionCount"/> Einträge). Grundlage der
    /// Honigwürfel auf dem Wegband. Nur im Speicher; nicht Teil des Chronik-Eintrags.
    /// </summary>
    public IReadOnlyList<TimeSpan> DistractionStarts { get; }

    /// <summary>
    /// Laufende Fokus-Serie: Fokuszeit seit der letzten Ablenkung. Abwesenheit (Leerlauf, Standby)
    /// unterbricht die Serie nicht, zählt aber auch nicht mit – nur eine Ablenkung setzt sie zurück.
    /// </summary>
    public TimeSpan CurrentStreak { get; private set; }

    /// <summary>Längste Fokus-Serie dieser Wacht.</summary>
    public TimeSpan LongestStreak { get; private set; }

    public TimeSpan Measured => Focused + Distracted + Away;

    public TimeSpan Remaining => Planned - Measured is var r && r > TimeSpan.Zero ? r : TimeSpan.Zero;

    public double Progress => Math.Clamp(Measured / Planned, 0, 1);

    /// <summary>Frostanteil: Ablenkung im Verhältnis zur gemessenen Zeit.</summary>
    public double FrostRatio => Measured <= TimeSpan.Zero ? 0 : Distracted / Measured;

    public bool IsFinished => Phase is SessionPhase.Completed or SessionPhase.Aborted;

    /// <summary>Verbucht die seit der letzten Messung vergangene Zeit und übernimmt den neuen Zustand.</summary>
    public void Update(ActivityState state, string? processName)
    {
        if (Phase != SessionPhase.Running)
        {
            return;
        }

        Accumulate();
        if (Phase != SessionPhase.Running)
        {
            return;
        }

        if (state == ActivityState.Distracted && CurrentState != ActivityState.Distracted)
        {
            DistractionCount++;
            _distractionStarts.Add(Measured);   // Speicher nur, wenn eine Episode beginnt
            CurrentStreak = TimeSpan.Zero;
        }

        CurrentState = state;
        CurrentProcess = processName;
    }

    public void Pause()
    {
        if (Phase != SessionPhase.Running)
        {
            return;
        }

        Accumulate();
        if (Phase == SessionPhase.Running)
        {
            Phase = SessionPhase.Paused;
        }
    }

    public void Resume()
    {
        if (Phase != SessionPhase.Paused)
        {
            return;
        }

        _lastTimestamp = _time.GetTimestamp();
        Phase = SessionPhase.Running;
    }

    public void Abort()
    {
        if (IsFinished)
        {
            return;
        }

        if (Phase == SessionPhase.Running)
        {
            Accumulate();
        }

        if (!IsFinished)
        {
            Finish(SessionPhase.Aborted);
        }
    }

    /// <summary>Erzeugt den unveränderlichen Datensatz für die Chronik.</summary>
    public SessionRecord ToRecord()
    {
        if (!IsFinished)
        {
            throw new InvalidOperationException("Nur beendete Wachten werden in die Chronik eingetragen.");
        }

        return new SessionRecord
        {
            Id = Id,
            StartedAtUtc = StartedAtUtc,
            EndedAtUtc = EndedAtUtc!.Value,
            Planned = Planned,
            Focused = Focused,
            Distracted = Distracted,
            Away = Away,
            DistractionCount = DistractionCount,
            Outcome = Phase,
            TopDistractions = TopDistractions(5),
            LongestFocusStreak = LongestStreak,
        };
    }

    public IReadOnlyList<DistractionEntry> TopDistractions(int count) =>
        _distractionsByProcess
            .OrderByDescending(p => p.Value)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .Take(count)
            .Select(p => new DistractionEntry(p.Key, p.Value))
            .ToList();

    private void Accumulate()
    {
        var now = _time.GetTimestamp();
        var delta = _time.GetElapsedTime(_lastTimestamp, now);
        _lastTimestamp = now;

        if (delta <= TimeSpan.Zero)
        {
            return;
        }

        // Nie mehr verbuchen als geplant – die Wacht endet exakt nach der geplanten Dauer.
        var remaining = Planned - Measured;
        var credited = delta < remaining ? delta : remaining;
        var state = delta > MaxTickGap ? ActivityState.Away : CurrentState;

        switch (state)
        {
            case ActivityState.Focused:
                Focused += credited;
                CurrentStreak += credited;
                if (CurrentStreak > LongestStreak)
                {
                    LongestStreak = CurrentStreak;
                }

                break;
            case ActivityState.Distracted:
                Distracted += credited;
                var key = string.IsNullOrWhiteSpace(CurrentProcess) ? "unbekannt" : CurrentProcess;
                _distractionsByProcess[key] = _distractionsByProcess.GetValueOrDefault(key) + credited;
                break;
            case ActivityState.Away:
                Away += credited;
                break;
        }

        if (Measured >= Planned)
        {
            Finish(SessionPhase.Completed);
        }
    }

    private void Finish(SessionPhase outcome)
    {
        Phase = outcome;
        EndedAtUtc = _time.GetUtcNow();
    }
}
