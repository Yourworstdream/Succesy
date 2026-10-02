namespace Laternenwacht.Core.Model;

/// <summary>Lebenszyklus einer Fokussitzung ("Wacht").</summary>
public enum SessionPhase
{
    Running,
    Paused,
    Completed,
    Aborted,
}
