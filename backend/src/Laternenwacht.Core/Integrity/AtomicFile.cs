namespace Laternenwacht.Core.Integrity;

/// <summary>
/// Schreibt Dateien atomar: erst in eine temporäre Datei, dann Umbenennen.
/// Ein Absturz während des Schreibens hinterlässt so nie eine halb geschriebene Datei.
/// </summary>
public static class AtomicFile
{
    public static void WriteAllText(string path, string contents) =>
        WriteAllBytes(path, System.Text.Encoding.UTF8.GetBytes(contents));

    public static void WriteAllBytes(string path, byte[] contents)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(contents);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";
        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(contents);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, path, overwrite: true);
    }
}
