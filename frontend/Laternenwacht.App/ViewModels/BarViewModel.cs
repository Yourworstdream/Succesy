namespace Laternenwacht.App.ViewModels;

/// <summary>Datenkontext der Fokusleiste: Anzeige der Wacht und Schnelleinstellungen des Kontextmenüs.</summary>
internal sealed class BarViewModel(SessionViewModel session, SettingsViewModel settings)
{
    public SessionViewModel Session { get; } = session;

    public SettingsViewModel Settings { get; } = settings;
}
