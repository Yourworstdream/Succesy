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

    /// <summary>Die zuletzt erfasste Momentaufnahme (für Anzeigezwecke).</summary>
    public ActivitySnapshot? LastSnapshot { get; private set; }

    public bool IsActive => Current is { IsFinished: false };

    /// <summary>Wird genau einmal ausgelöst, wenn eine Wacht endet (abgeschlossen oder abgebrochen).</summary>
    public event EventHandler<SessionRecord>? SessionEnded;

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
        Current = new FocusSession(duration, _time, _classifier.Classify(snapshot), snapshot.ProcessName);
        return Current;
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
        session.Update(_classifier.Classify(snapshot), snapshot.ProcessName);
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

    private void RaiseIfFinished(FocusSession session)
    {
        if (session.IsFinished && ReferenceEquals(session, Current) && !_reported.Contains(session.Id))
        {
            _reported.Add(session.Id);
            SessionEnded?.Invoke(this, session.ToRecord());
        }
    }
}
