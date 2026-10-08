namespace Laternenwacht.App.ViewModels;

/// <summary>Datenkontext des Hauptfensters ("Kammer des Wächters").</summary>
internal sealed class ShellViewModel(SessionViewModel session, ChronicleViewModel chronicle, SettingsViewModel settings) : ObservableObject
{
    public SessionViewModel Session { get; } = Connect(session, chronicle);

    public ChronicleViewModel Chronicle { get; } = chronicle;

    public SettingsViewModel Settings { get; } = settings;

    /// <summary>Sicherheits- und Datenschutzmerkmale für die Seite "Über".</summary>
    public static IReadOnlyList<string> SecurityFacts { get; } =
    [
        "Keine Netzwerkverbindung, keine Telemetrie, keine Administratorrechte.",
        "Erfasst wird nur der Name des Programms im Vordergrund und die Leerlaufzeit – keine Fenstertitel, keine Tastatureingaben.",
        "Die Chronik ist mit HMAC-SHA256 versiegelt und verkettet; ein Anker erkennt abgeschnittene Einträge.",
        "Der Siegelschlüssel ist per Windows-Datenschutz-API (DPAPI) an dein Benutzerkonto gebunden.",
        "Zeiten werden mit einer monotonen Uhr gemessen – das Verstellen der Systemzeit ändert nichts.",
        "Alle Dateien werden atomar geschrieben und beim Laden streng geprüft.",
    ];

    public string DataDirectory { get; } = Laternenwacht.Platform.Windows.AppPaths.DataDirectory;

    public string VersionText { get; } =
        "Version " + (typeof(ShellViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");

    /// <summary>
    /// Das Abschlussbild einer Wacht vergleicht ihre längste Serie mit der bisherigen Bestleistung der Chronik
    /// (abgefragt, bevor die Wacht eingetragen wird).
    /// </summary>
    private static SessionViewModel Connect(SessionViewModel session, ChronicleViewModel chronicle)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(chronicle);
        session.PreviousBestStreak = () => chronicle.BestStreak;
        return session;
    }
}
