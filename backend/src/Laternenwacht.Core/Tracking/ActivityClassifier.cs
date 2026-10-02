using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.Core.Tracking;

/// <summary>Bewertet eine Aktivitätsmomentaufnahme anhand der Einstellungen.</summary>
public sealed class ActivityClassifier
{
    private readonly HashSet<string> _allowed;
    private readonly HashSet<string> _distracting;
    private readonly ClassificationMode _mode;
    private readonly TimeSpan _idleThreshold;
    private readonly string _selfProcessName;

    public ActivityClassifier(FocusSettings settings, string selfProcessName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(selfProcessName);

        _allowed = new HashSet<string>(settings.AllowedProcesses.Select(ProcessNames.Normalize), StringComparer.Ordinal);
        _distracting = new HashSet<string>(settings.DistractingProcesses.Select(ProcessNames.Normalize), StringComparer.Ordinal);
        _mode = settings.Mode;
        _idleThreshold = settings.IdleThreshold;
        _selfProcessName = ProcessNames.Normalize(selfProcessName);
    }

    public ActivityState Classify(ActivitySnapshot snapshot)
    {
        if (snapshot.IdleTime >= _idleThreshold)
        {
            return ActivityState.Away;
        }

        // Unbekannter Vordergrund (Sperrbildschirm, Desktop, Zugriff verweigert) wird nicht bestraft.
        if (string.IsNullOrWhiteSpace(snapshot.ProcessName))
        {
            return ActivityState.Focused;
        }

        var name = ProcessNames.Normalize(snapshot.ProcessName);
        if (name == _selfProcessName || _allowed.Contains(name))
        {
            return ActivityState.Focused;
        }

        if (_distracting.Contains(name))
        {
            return ActivityState.Distracted;
        }

        return _mode == ClassificationMode.AllowList ? ActivityState.Distracted : ActivityState.Focused;
    }
}
