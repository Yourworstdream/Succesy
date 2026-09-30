using System.Globalization;

namespace Laternenwacht.App.Services;

/// <summary>
/// Minimales Fehlerprotokoll. Protokolliert werden nur technische Fehler,
/// niemals Aktivitäts- oder Prozessdaten des Benutzers.
/// </summary>
internal static class AppLog
{
    private const long MaxSize = 1024 * 1024;
    private static readonly object Sync = new();

    public static void Error(string context, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Write($"FEHLER [{context}] {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");
    }

    public static void Info(string message) => Write("INFO " + message);

    private static void Write(string line)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(AppPaths.DataDirectory);
                if (File.Exists(AppPaths.LogFile) && new FileInfo(AppPaths.LogFile).Length > MaxSize)
                {
                    File.Move(AppPaths.LogFile, AppPaths.LogFile + ".alt", overwrite: true);
                }

                var stamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                File.AppendAllText(AppPaths.LogFile, $"{stamp} {line}{Environment.NewLine}");
            }
        }
        catch (IOException)
        {
            // Protokollieren darf die Anwendung niemals zum Absturz bringen.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
