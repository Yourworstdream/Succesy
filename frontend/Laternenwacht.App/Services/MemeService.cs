using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Laternenwacht.App.Views;
using Laternenwacht.Core.Media;
using Laternenwacht.Core.Tracking;
using Laternenwacht.Platform.Windows;

namespace Laternenwacht.App.Services;

/// <summary>
/// Lässt bei jeder neuen Ablenkung ein Meme in der Schachtel der Königin über den Bildschirm treiben.
/// Quellen: eingebettete Memes (Assets\Memes) und der eigene Meme-Ordner des Benutzers.
/// Es treibt höchstens ein Meme gleichzeitig; dasselbe Meme kommt nie zweimal hintereinander.
/// </summary>
internal sealed class MemeService
{
    private const string ResourcePrefix = "Memes/";
    private const int DecodeWidth = 480;
    private static readonly TimeSpan SwimDuration = TimeSpan.FromSeconds(14);

    private ShuffleBag<MemeSource> _bag = new([]);
    private MemeFloatWindow? _current;

    public MemeService() => Reload();

    public int BuiltInCount { get; private set; }

    public int CustomCount { get; private set; }

    /// <summary>Liest eingebaute und eigene Memes neu ein (z. B. nach dem Hinzufügen).</summary>
    public void Reload()
    {
        var assembly = typeof(MemeService).Assembly;
        var builtIn = assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Select(n => new MemeSource(n, () => assembly.GetManifestResourceStream(n)))
            .ToList();
        var custom = MemeCatalog.Scan(AppPaths.MemeDirectory)
            .Select(path => new MemeSource(path, () => OpenUserFile(path)))
            .ToList();

        BuiltInCount = builtIn.Count;
        CustomCount = custom.Count;
        _bag = new ShuffleBag<MemeSource>(builtIn.Concat(custom));
    }

    /// <summary>Schickt ein Meme auf die Reise – sofern nicht gerade eines treibt.</summary>
    public void Show(DistractionStarted distraction)
    {
        ArgumentNullException.ThrowIfNull(distraction);
        if (_current is not null)
        {
            return;
        }

        // Unlesbare Bilder überspringen, aber nicht endlos suchen.
        for (var attempt = 0; attempt < Math.Min(_bag.Count, 5); attempt++)
        {
            if (!_bag.TryNext(out var source) || Decode(source) is not { } image)
            {
                continue;
            }

            // Jede neue Verlockung ist ein Stück aus der Schachtel der Königin.
            var caption = distraction.ProcessName is { Length: > 0 } p
                ? $"Türkischer Honig Nr. {distraction.Episode} · {p}"
                : $"Türkischer Honig Nr. {distraction.Episode}";
            var window = new MemeFloatWindow(image, caption, SwimDuration);
            window.Closed += (_, _) => _current = null;
            _current = window;
            window.Show();
            return;
        }
    }

    /// <summary>Lässt ein gerade treibendes Meme versinken (z. B. bei der Rückkehr zur Arbeit).</summary>
    public void SinkCurrent() => _current?.Sink();

    private static FileStream? OpenUserFile(string path)
    {
        // Erneute Größenprüfung: Die Datei könnte sich seit dem Einlesen geändert haben.
        var info = new FileInfo(path);
        return info.Exists && info.Length <= MemeCatalog.MaxFileSizeBytes ? info.OpenRead() : null;
    }

    private static BitmapImage? Decode(MemeSource source)
    {
        try
        {
            using var stream = source.Open();
            if (stream is null)
            {
                return null;
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;   // Datei sofort wieder freigeben
            image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            image.DecodePixelWidth = DecodeWidth;           // begrenzt den Speicherbedarf
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
                                       or FileFormatException or InvalidOperationException or ArgumentException)
        {
            AppLog.Error("Meme laden", ex);
            return null;
        }
    }

    private sealed record MemeSource(string Name, Func<Stream?> Open);
}
