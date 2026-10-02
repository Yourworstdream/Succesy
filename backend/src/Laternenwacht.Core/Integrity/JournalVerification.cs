namespace Laternenwacht.Core.Integrity;

public enum SealStatus
{
    /// <summary>Noch keine Einträge – nichts zu prüfen.</summary>
    Empty,

    /// <summary>Alle Siegel sind unversehrt.</summary>
    Intact,

    /// <summary>Mindestens ein Siegel ist gebrochen (Manipulation oder Beschädigung).</summary>
    Broken,
}

/// <summary>Ergebnis der Integritätsprüfung des Journals.</summary>
public sealed record JournalVerification(SealStatus Status, int ValidEntries, long? BrokenAtSeq, string Message)
{
    public bool IsTrustworthy => Status is SealStatus.Empty or SealStatus.Intact;
}
