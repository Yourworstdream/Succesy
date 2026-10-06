namespace Laternenwacht.Core.Settings;

/// <summary>Fachliche Änderungen an Einstellungen, die die Oberfläche per Schnellaktion auslöst.</summary>
public static class SettingsEditing
{
    /// <summary>
    /// Trägt ein Programm als Verlockung ein und entfernt es zugleich aus den Gefährten.
    /// Liefert <c>null</c>, wenn der Name ungültig ist oder die Liste voll ist.
    /// </summary>
    public static FocusSettings? MarkAsDistraction(FocusSettings settings, string processName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(processName);

        var name = ProcessNames.Normalize(processName);
        if (!ProcessNames.IsValid(name))
        {
            return null;
        }

        var distracting = settings.DistractingProcesses.Append(name).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var allowed = settings.AllowedProcesses.Where(a => a != name).ToList();
        var updated = settings with { DistractingProcesses = distracting, AllowedProcesses = allowed };
        return SettingsValidator.Validate(updated).Count == 0 ? updated : null;
    }
}
