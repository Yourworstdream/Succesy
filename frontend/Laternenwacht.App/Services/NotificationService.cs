using System.Media;
using Laternenwacht.App.Views;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Services;

/// <summary>Überbringt Mahnrufe als Push-Benachrichtigung ("Rabenbote"). Es ist stets höchstens eine sichtbar.</summary>
internal sealed class NotificationService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(10);

    private RavenToastWindow? _current;

    public void Show(Admonition admonition)
    {
        ArgumentNullException.ThrowIfNull(admonition);

        _current?.FlyOut();
        var toast = new RavenToastWindow(admonition.Text, $"— aus »{ChronicleBooks.Title(admonition.Book)}«", Lifetime);
        toast.Closed += (_, _) =>
        {
            if (ReferenceEquals(_current, toast))
            {
                _current = null;
            }
        };
        _current = toast;
        toast.Show();
        SystemSounds.Asterisk.Play();
    }

    public void CloseAll() => _current?.FlyOut();
}
