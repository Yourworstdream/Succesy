namespace Laternenwacht.App.ViewModels;

/// <summary>Datenkontext des Hauptfensters ("Kammer des Wächters").</summary>
internal sealed class ShellViewModel(SessionViewModel session, ChronicleViewModel chronicle, SettingsViewModel settings) : ObservableObject
{
    public SessionViewModel Session { get; } = session;

    public ChronicleViewModel Chronicle { get; } = chronicle;

    public SettingsViewModel Settings { get; } = settings;

    public string DataDirectory { get; } = Laternenwacht.Platform.Windows.AppPaths.DataDirectory;

    public string VersionText { get; } =
        "Version " + (typeof(ShellViewModel).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
}
