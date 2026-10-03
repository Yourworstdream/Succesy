using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Settings;

/// <summary>
/// Benutzereinstellungen des Wächters.
/// </summary>
/// <remarks>
/// Die Eigenschaften sind bewusst <c>set</c> statt <c>init</c>: Der JSON-Quellgenerator belegt fehlende
/// <c>init</c>-Eigenschaften mit <c>default</c> statt mit dem Initialisierer – ältere Einstellungsdateien
/// würden sonst beim Update ungültig. Änderungen erfolgen im Code ausschließlich über <c>with</c>.
/// </remarks>
public sealed record FocusSettings
{
    public static readonly TimeSpan MinDuration = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan MaxDuration = TimeSpan.FromHours(8);
    public static readonly TimeSpan MinIdleThreshold = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MaxIdleThreshold = TimeSpan.FromMinutes(60);
    public const int MaxListEntries = 200;
    public const double MaxScreenCoordinate = 100_000;

    /// <summary>Standarddauer einer Wacht.</summary>
    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(25);

    /// <summary>Ab dieser Leerlaufzeit gilt der Benutzer als abwesend.</summary>
    public TimeSpan IdleThreshold { get; set; } = TimeSpan.FromMinutes(2);

    public ClassificationMode Mode { get; set; } = ClassificationMode.BlockList;

    /// <summary>"Gefährten": Programme, die der Arbeit dienen.</summary>
    public IReadOnlyList<string> AllowedProcesses { get; set; } =
        ["devenv", "code", "rider64", "winword", "excel", "powerpnt", "onenote", "notepad", "windowsterminal", "explorer"];

    /// <summary>"Verlockungen": Programme, die vom Weg abbringen.</summary>
    public IReadOnlyList<string> DistractingProcesses { get; set; } =
        ["discord", "steam", "epicgameslauncher", "spotify", "whatsapp", "telegram", "netflix", "battle.net"];

    /// <summary>Soll die Leiste am oberen Rand stets sichtbar sein?</summary>
    public bool BarAlwaysOnTop { get; set; } = true;

    /// <summary>Aus welchem Buch der Chroniken die Mahnrufe stammen.</summary>
    public ChronicleBook SayingsBook { get; set; } = ChronicleBook.All;

    /// <summary>Mahnrufe als Benachrichtigung ("Rabenbote") unten rechts einblenden.</summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>Bei jeder neuen Ablenkung treibt ein Meme über den Bildschirm.</summary>
    public bool ShowMemes { get; set; } = true;

    /// <summary>Vom Benutzer verschobene Position der Leiste (geräteunabhängige Pixel); <c>null</c> = oben angedockt.</summary>
    public double? BarLeft { get; set; }

    /// <inheritdoc cref="BarLeft"/>
    public double? BarTop { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasCustomBarPosition => BarLeft is not null && BarTop is not null;

    /// <summary>
    /// Standardwerte. Liefert bei jedem Zugriff eine neue Instanz, da die Eigenschaften setzbar sind
    /// (nötig, damit fehlende Felder älterer Einstellungsdateien ihre Standardwerte behalten).
    /// </summary>
    public static FocusSettings Default => new();
}
