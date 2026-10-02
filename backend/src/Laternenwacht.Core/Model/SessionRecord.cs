namespace Laternenwacht.Core.Model;

/// <summary>Unveränderliches Ergebnis einer abgeschlossenen Fokussitzung.</summary>
public sealed record SessionRecord
{
    public required Guid Id { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset EndedAtUtc { get; init; }

    public required TimeSpan Planned { get; init; }

    public required TimeSpan Focused { get; init; }

    public required TimeSpan Distracted { get; init; }

    public required TimeSpan Away { get; init; }

    public required int DistractionCount { get; init; }

    public required SessionPhase Outcome { get; init; }

    public IReadOnlyList<DistractionEntry> TopDistractions { get; init; } = [];

    /// <summary>Gemessene Gesamtzeit (ohne Pausen).</summary>
    public TimeSpan Measured => Focused + Distracted + Away;

    /// <summary>Anteil der Fokuszeit an der gemessenen Zeit (0..1).</summary>
    public double FocusRatio => Measured <= TimeSpan.Zero ? 0 : Focused / Measured;
}

/// <summary>Kumulierte Ablenkungszeit je Programm.</summary>
public sealed record DistractionEntry(string ProcessName, TimeSpan Duration);
