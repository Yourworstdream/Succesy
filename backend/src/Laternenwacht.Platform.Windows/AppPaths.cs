namespace Laternenwacht.Platform.Windows;

/// <summary>Ablageorte im Benutzerprofil. Es werden keine Daten außerhalb von %LOCALAPPDATA% geschrieben.</summary>
public static class AppPaths
{
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Laternenwacht");

    public static string SettingsFile => Path.Combine(DataDirectory, "einstellungen.json");

    public static string LogFile => Path.Combine(DataDirectory, "laternenwacht.log");
}
