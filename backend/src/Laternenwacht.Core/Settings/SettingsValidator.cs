using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Settings;

/// <summary>Prüft Einstellungen gegen fachliche Grenzen (Eingabevalidierung).</summary>
public static class SettingsValidator
{
    public static IReadOnlyList<string> Validate(FocusSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = new List<string>();

        if (settings.DefaultDuration < FocusSettings.MinDuration || settings.DefaultDuration > FocusSettings.MaxDuration)
        {
            errors.Add($"Die Dauer einer Wacht muss zwischen {FocusSettings.MinDuration.TotalMinutes:0} und {FocusSettings.MaxDuration.TotalMinutes:0} Minuten liegen.");
        }

        if (settings.IdleThreshold < FocusSettings.MinIdleThreshold || settings.IdleThreshold > FocusSettings.MaxIdleThreshold)
        {
            errors.Add($"Die Abwesenheitsschwelle muss zwischen {FocusSettings.MinIdleThreshold.TotalSeconds:0} Sekunden und {FocusSettings.MaxIdleThreshold.TotalMinutes:0} Minuten liegen.");
        }

        if (!Enum.IsDefined(settings.Mode))
        {
            errors.Add("Unbekannter Bewertungsmodus.");
        }

        if (!Enum.IsDefined(settings.SayingsBook))
        {
            errors.Add("Unbekanntes Buch der Chroniken.");
        }

        if (!Enum.IsDefined(settings.BarSize))
        {
            errors.Add("Unbekannte Größe der Fokusleiste.");
        }

        if (!IsValidCoordinate(settings.BarLeft) || !IsValidCoordinate(settings.BarTop)
            || (settings.BarLeft is null) != (settings.BarTop is null))
        {
            errors.Add("Die gespeicherte Position der Fokusleiste ist ungültig.");
        }

        ValidateList(settings.AllowedProcesses, "Gefährten", errors);
        ValidateList(settings.DistractingProcesses, "Verlockungen", errors);

        if (settings.AllowedProcesses is not null && settings.DistractingProcesses is not null)
        {
            var overlap = settings.AllowedProcesses.Intersect(settings.DistractingProcesses, StringComparer.Ordinal).ToList();
            if (overlap.Count > 0)
            {
                errors.Add($"Ein Programm kann nicht zugleich Gefährte und Verlockung sein: {string.Join(", ", overlap)}");
            }
        }

        if (settings.Mode == ClassificationMode.AllowList && settings.AllowedProcesses is { Count: 0 })
        {
            errors.Add("Im Modus \"Nur Gefährten\" muss mindestens ein Gefährte eingetragen sein.");
        }

        return errors;
    }

    private static bool IsValidCoordinate(double? value) =>
        value is null || (double.IsFinite(value.Value) && Math.Abs(value.Value) <= FocusSettings.MaxScreenCoordinate);

    private static void ValidateList(IReadOnlyList<string>? list, string label, List<string> errors)
    {
        if (list is null)
        {
            errors.Add($"Die Liste der {label} fehlt.");
            return;
        }

        if (list.Count > FocusSettings.MaxListEntries)
        {
            errors.Add($"Die Liste der {label} darf höchstens {FocusSettings.MaxListEntries} Einträge enthalten.");
        }

        var invalid = list.Where(n => n is null || !ProcessNames.IsValid(n)).ToList();
        if (invalid.Count > 0)
        {
            errors.Add($"Ungültige Einträge bei den {label}: {string.Join(", ", invalid.Select(i => $"\"{i}\""))}");
        }
    }
}
