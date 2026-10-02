using System.Text.Json;
using Laternenwacht.Core.Integrity;

namespace Laternenwacht.Core.Settings;

/// <summary>
/// Lädt und speichert Einstellungen als JSON.
/// Beschädigte oder ungültige Dateien führen nie zum Absturz, sondern zu sicheren Standardwerten.
/// </summary>
public sealed class SettingsStore(string filePath)
{
    private const long MaxFileSize = 256 * 1024;

    public string FilePath { get; } = filePath ?? throw new ArgumentNullException(nameof(filePath));

    /// <summary>Hinweis, falls beim letzten Laden auf Standardwerte zurückgefallen wurde.</summary>
    public string? LastLoadWarning { get; private set; }

    public FocusSettings Load()
    {
        LastLoadWarning = null;

        if (!File.Exists(FilePath))
        {
            return FocusSettings.Default;
        }

        try
        {
            if (new FileInfo(FilePath).Length > MaxFileSize)
            {
                return Fallback("Die Einstellungsdatei ist unplausibel groß und wurde ignoriert.");
            }

            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize(json, SettingsJsonContext.Default.FocusSettings);
            if (settings is null)
            {
                return Fallback("Die Einstellungsdatei ist leer.");
            }

            var errors = SettingsValidator.Validate(settings);
            return errors.Count == 0
                ? settings
                : Fallback("Die Einstellungen waren ungültig: " + string.Join(" ", errors));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return Fallback("Die Einstellungsdatei konnte nicht gelesen werden: " + ex.Message);
        }
    }

    public void Save(FocusSettings settings)
    {
        var errors = SettingsValidator.Validate(settings);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(Environment.NewLine, errors), nameof(settings));
        }

        var json = JsonSerializer.Serialize(settings, SettingsJsonContext.Default.FocusSettings);
        AtomicFile.WriteAllText(FilePath, json);
    }

    private FocusSettings Fallback(string warning)
    {
        LastLoadWarning = warning;
        return FocusSettings.Default;
    }
}
