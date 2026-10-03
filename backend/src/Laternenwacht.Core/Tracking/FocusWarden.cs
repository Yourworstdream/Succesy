using Laternenwacht.Core.Abstractions;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.Core.Tracking;

/// <summary>
/// Der "Wächter": verbindet Messung, Bewertung und Sitzung.
/// Der Aufrufer (UI-Timer) ruft <see cref="Pulse"/> in regelmäßigen Abständen auf.
/// </summary>
public sealed class FocusWarden
{
    private readonly IActivityProbe _probe;
    private readonly TimeProvider _time;
    private readonly string _selfProcessName;
    private readonly HashSet<Guid> _reported = [];
    private TimeSpan _episodeFrostStart;
    private int _praisedMilestones;
    private ActivityClassifier _classifier;

    public FocusWarden(IActivityProbe probe, TimeProvider time, FocusSettings settings, string selfProcessName)
    {
        _probe = probe ?? throw new ArgumentNullException(nameof(probe));
        _time = time ?? throw new ArgumentNullException(nameof(time));
        _selfProcessName = selfProcessName ?? throw new ArgumentNullException(nameof(selfProcessName));
        _classifier = new ActivityClassifier(settings, selfProcessName);
    }

    /// <summary>Die laufende oder zuletzt beendete Wacht.</summary>
    public FocusSession? Current { get; private set; }

    /// <summary>Prozessname der eigenen Anwendung (wird nie als Ablenkung gewertet).</summary>
    public string SelfProcessName => _selfProcessName;

    /// <summary>Die zuletzt erfasste Momentaufnahme (für Anzeigezwecke).</summary>
    public ActivitySnapshot? LastSnapshot { get; private set; }

    public bool IsActive => Current is { IsFinished: false };

    /// <summary>Wird genau einmal ausgelöst, wenn eine Wacht endet (abgeschlossen oder abgebrochen).</summary>
    public event EventHandler<SessionRecord>? SessionEnded;

    /// <summary>
    /// Wird bei jeder neuen Ablenkungs-Episode ausgelöst (Wechsel in den Zustand "abgelenkt"),
    /// auch wenn die Wacht bereits abgelenkt beginnt. Anhaltende Ablenkung löst es nicht erneut aus.
    /// </summary>
    public event EventHandler<DistractionStarted>? DistractionStarted;

    /// <summary>
    /// Wird ausgelöst, wenn der Benutzer nach einer Ablenkung von mindestens
    /// <see cref="MinimumAbsenceForWelcome"/> zur Arbeit zurückkehrt (Wechsel "abgelenkt" → "Fokus").
    /// </summary>
    public event EventHandler<ReturnedToWork>? ReturnedToWork;

    /// <summary>Eine Fokus-Serie hat eine Schwelle aus <see cref="Praises.StreakMilestones"/> erreicht (je Serie einmal).</summary>
    public event EventHandler<FocusStreakReached>? FocusStreakReached;

    /// <summary>Kürzere Abstecher (z. B. ein versehentliches Alt+Tab) werden nicht begrüßt.</summary>
    public static readonly TimeSpan MinimumAbsenceForWelcome = TimeSpan.FromSeconds(10);

    /// <summary>Übernimmt geänderte Einstellungen; eine laufende Wacht wird ab sofort danach bewertet.</summary>
    public void ApplySettings(FocusSettings settings) =>
        _classifier = new ActivityClassifier(settings, _selfProcessName);

    public FocusSession Start(TimeSpan duration)
    {
        if (IsActive)
        {
            throw new InvalidOperationException("Es läuft bereits eine Wacht.");
        }

        var snapshot = _probe.Capture();
        LastSnapshot = snapshot;
        var session = new FocusSession(duration, _time, _classifier.Classify(snapshot), snapshot.ProcessName);
        Current = session;
        _praisedMilestones = 0;
        RaiseIfNewDistraction(session, previousCount: 0);
        return session;
    }

    /// <summary>Misst einmal und aktualisiert die laufende Wacht.</summary>
    public void Pulse()
    {
        if (Current is not { Phase: SessionPhase.Running } session)
        {
            return;
        }

        var snapshot = _probe.Capture();
        LastSnapshot = snapshot;
        var previousCount = session.DistractionCount;
        var previousState = session.CurrentState;
        var previousProcess = session.CurrentProcess;
        session.Update(_classifier.Classify(snapshot), snapshot.ProcessName);
        RaiseIfNewDistraction(session, previousCount);
        RaiseIfReturned(session, previousState, previousProcess);
        RaiseIfStreakMilestone(session);
        RaiseIfFinished(session);
    }

    public void Pause()
    {
        if (Current is { } session)
        {
            session.Pause();
            RaiseIfFinished(session);
        }
    }

    public void Resume() => Current?.Resume();

    public void Abort()
    {
        if (Current is { IsFinished: false } session)
        {
            session.Abort();
            RaiseIfFinished(session);
        }
    }

    private void RaiseIfNewDistraction(FocusSession session, int previousCount)
    {
        if (session.DistractionCount > previousCount && session.Phase == SessionPhase.Running)
        {
            _episodeFrostStart = session.Distracted;
            DistractionStarted?.Invoke(this, new DistractionStarted(session.CurrentProcess, session.DistractionCount));
        }
    }

    private void RaiseIfStreakMilestone(FocusSession session)
    {
        if (session.CurrentStreak == TimeSpan.Zero)
        {
            _praisedMilestones = 0;   // neue Serie – alle Schwellen sind wieder erreichbar
            return;
        }

        // Nur die höchste neu erreichte Schwelle loben (z. B. nach Standby keine Kaskade).
        var reached = Praises.StreakMilestones.Count(m => session.CurrentStreak >= TimeSpan.FromMinutes(m));
        if (reached > _praisedMilestones && session.Phase == SessionPhase.Running)
        {
            _praisedMilestones = reached;
            FocusStreakReached?.Invoke(this, new FocusStreakReached(Praises.StreakMilestones[reached - 1], reached - 1));
        }
    }

    private void RaiseIfReturned(FocusSession session, ActivityState previousState, string? previousProcess)
    {
        if (previousState != ActivityState.Distracted || session.CurrentState != ActivityState.Focused
            || session.Phase != SessionPhase.Running)
        {
            return;
        }

        var absence = session.Distracted - _episodeFrostStart;
        if (absence >= MinimumAbsenceForWelcome)
        {
            ReturnedToWork?.Invoke(this, new ReturnedToWork(previousProcess, absence, session.DistractionCount));
        }
    }

    private void RaiseIfFinished(FocusSession session)
    {
        if (session.IsFinished && ReferenceEquals(session, Current) && !_reported.Contains(session.Id))
        {
            _reported.Add(session.Id);
            SessionEnded?.Invoke(this, session.ToRecord());
        }
    }
}

/// <summary>Eine neue Ablenkungs-Episode hat begonnen.</summary>
/// <param name="ProcessName">Das verlockende Programm (ohne ".exe") oder <c>null</c>, wenn unbekannt.</param>
/// <param name="Episode">Laufende Nummer der Ablenkung in dieser Wacht (1, 2, 3 …).</param>
public sealed record DistractionStarted(string? ProcessName, int Episode);

/// <summary>Der Benutzer ist nach einer Ablenkung zur Arbeit zurückgekehrt.</summary>
/// <param name="ProcessName">Die Verlockung, von der er zurückkehrt (ohne ".exe") oder <c>null</c>.</param>
/// <param name="Absence">Dauer dieser Ablenkungs-Episode.</param>
/// <param name="Episode">Nummer der beendeten Ablenkung in dieser Wacht.</param>
public sealed record ReturnedToWork(string? ProcessName, TimeSpan Absence, int Episode);
