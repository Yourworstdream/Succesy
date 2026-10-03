using System.Windows.Input;

namespace Laternenwacht.App.ViewModels;

/// <summary>Datenkontext der Fokusleiste: Anzeige der Wacht und Schnelleinstellungen des Kontextmenüs.</summary>
internal sealed class BarViewModel
{
    public BarViewModel(SessionViewModel session, SettingsViewModel settings)
    {
        Session = session ?? throw new ArgumentNullException(nameof(session));
        Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        MarkAsDistractionCommand = new RelayCommand(
            () =>
            {
                if (Session.MarkCandidate is { } process)
                {
                    Settings.MarkAsDistraction(process);
                }
            },
            () => Session.HasMarkCandidate);
    }

    public SessionViewModel Session { get; }

    public SettingsViewModel Settings { get; }

    /// <summary>Markiert das Programm im Vordergrund als Verlockung.</summary>
    public ICommand MarkAsDistractionCommand { get; }
}
