namespace Laternenwacht.App.Services;

/// <summary>Ablageorte im Benutzerprofil. Es werden keine Daten außerhalb von %LOCALAPPDATA% geschrieben.</summary>
internal static class AppPaths
{
    public static string DataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Laternenwacht");

    public static string SettingsFile => Path.Combine(DataDirectory, "einstellungen.json");

    public static string LogFile => Path.Combine(DataDirectory, "laternenwacht.log");
}
