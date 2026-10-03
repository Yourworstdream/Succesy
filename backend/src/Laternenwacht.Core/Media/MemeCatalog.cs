namespace Laternenwacht.Core.Media;

/// <summary>
/// Findet eigene Meme-Bilder im Meme-Ordner des Benutzers.
/// Eingelesen wird defensiv: nur bekannte Bildendungen, nur die oberste Ordnerebene,
/// keine Verknüpfungen/Reparse-Points, Größen- und Anzahlgrenzen. Ein fehlender oder
/// unlesbarer Ordner ergibt einfach eine leere Liste.
/// </summary>
public static class MemeCatalog
{
    public const long MaxFileSizeBytes = 10L * 1024 * 1024;
    public const int MaxFiles = 200;

    public static IReadOnlySet<string> AllowedExtensions { get; } =
        new HashSet<string>([".jpg", ".jpeg", ".png", ".bmp", ".gif"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Liefert die Pfade gültiger Bilddateien, alphabetisch sortiert.</summary>
    public static IReadOnlyList<string> Scan(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);

        if (!Directory.Exists(directory))
        {
            return [];
        }

        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = false,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
            };

            return new DirectoryInfo(directory)
                .EnumerateFiles("*", options)
                .Where(f => AllowedExtensions.Contains(f.Extension))
                .Where(f => f.Length is > 0 and <= MaxFileSizeBytes)
                .Select(f => f.FullName)
                .Order(StringComparer.OrdinalIgnoreCase)
                .Take(MaxFiles)
                .ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return [];
        }
    }
}
