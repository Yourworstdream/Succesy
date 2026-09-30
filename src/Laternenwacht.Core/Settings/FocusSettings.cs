using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Settings;

/// <summary>Benutzereinstellungen des Wächters.</summary>
public sealed record FocusSettings
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);
    public static readonly TimeSpan MinIdleThreshold = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxIdleThreshold = TimeSpan.FromMinutes(60);
    public const int MaxListEntries = 200;

    /// <summary>Standarddauer einer Wacht.</summary>
    public TimeSpan DefaultDuration { get; init; } = TimeSpan.FromMinutes(25);

    /// <summary>Ab dieser Leerlaufzeit gilt der Benutzer als abwesend.</summary>
    public TimeSpan IdleThreshold { get; init; } = TimeSpan.FromMinutes(2);

    public ClassificationMode Mode { get; init; } = ClassificationMode.BlockList;

    /// <summary>"Gefährten": Programme, die der Arbeit dienen.</summary>
    public IReadOnlyList<string> AllowedProcesses { get; init; } =
        ["devenv", "code", "rider64", "winword", "excel", "powerpnt", "onenote", "notepad", "windowsterminal", "explorer"];

    /// <summary>"Verlockungen": Programme, die vom Weg abbringen.</summary>
    public IReadOnlyList<string> DistractingProcesses { get; init; } =
        ["discord", "steam", "epicgameslauncher", "spotify", "whatsapp", "telegram", "netflix", "battle.net"];

    /// <summary>Soll die Leiste am oberen Rand stets sichtbar sein?</summary>
    public bool BarAlwaysOnTop { get; init; } = true;

    public static FocusSettings Default { get; } = new();
}
