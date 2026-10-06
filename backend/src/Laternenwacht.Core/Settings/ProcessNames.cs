using System.Text.RegularExpressions;

namespace Laternenwacht.Core.Settings;

/// <summary>
/// Normalisierung und Validierung von Prozessnamen.
/// Eingaben werden streng geprüft, damit keine Pfade oder Steuerzeichen in Listen gelangen.
/// </summary>
public static partial class ProcessNames
{
    public const int MaxLength = 64;

    private static readonly char[] Separators = [',', ';', '\n', '\r'];

    [GeneratedRegex(@"^[a-z0-9][a-z0-9 ._\-]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidPattern();

    /// <summary>Kleinschreibung, ohne Leerraum am Rand und ohne Endung ".exe".</summary>
    public static string Normalize(string raw)
    {
        ArgumentNullException.ThrowIfNull(raw);

        var name = raw.Trim().ToLowerInvariant();
        if (name.EndsWith(".exe", StringComparison.Ordinal))
        {
            name = name[..^4];
        }

        return name.Trim();
    }

    /// <summary>
    /// Leitet aus einem Programmpfad den Prozessnamen ab – so, wie ihn auch <c>Process.ProcessName</c> liefert:
    /// Dateiname ohne Verzeichnis und ohne Endung ".exe"; die Schreibweise bleibt erhalten.
    /// Gibt <c>null</c> zurück, wenn der Pfad keinen Dateinamen enthält.
    /// </summary>
    public static string? FromImagePath(ReadOnlySpan<char> path)
    {
        var name = path[(path.LastIndexOfAny('\\', '/') + 1)..];
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            name = name[..^4];
        }

        name = name.Trim();
        return name.IsEmpty ? null : name.ToString();
    }

    public static bool IsValid(string normalized) =>
        !string.IsNullOrEmpty(normalized) && ValidPattern().IsMatch(normalized);

    /// <summary>
    /// Zerlegt eine Benutzereingabe (Komma, Semikolon oder Zeilenumbruch getrennt)
    /// in normalisierte, eindeutige Namen. Ungültige Einträge werden gesammelt zurückgegeben.
    /// </summary>
    public static (IReadOnlyList<string> Valid, IReadOnlyList<string> Invalid) ParseList(string? input)
    {
        var valid = new SortedSet<string>(StringComparer.Ordinal);
        var invalid = new List<string>();

        if (string.IsNullOrWhiteSpace(input))
        {
            return ([], []);
        }

        foreach (var part in input.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var name = Normalize(part);
            if (IsValid(name))
            {
                valid.Add(name);
            }
            else
            {
                invalid.Add(part);
            }
        }

        return (valid.ToList(), invalid);
    }
}
