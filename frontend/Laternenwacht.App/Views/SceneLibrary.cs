using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Laternenwacht.App.Views;

/// <summary>
/// Die sechs Szenenbilder der Reise (eigene Zeichnungen, 960 × 1200, eingebettet unter Assets/Szenen).
/// </summary>
/// <remarks>
/// Jedes Bild wird erst beim ersten Zugriff geladen, auf 960 Pixel Breite dekodiert, sofort vollständig eingelesen
/// (die Datei bleibt nicht geöffnet) und eingefroren – danach teilen sich alle Ansichten dieselbe Bitmap,
/// auch über Threads hinweg. Wer nie das Ende einer Wacht sieht, lädt die Endbilder nie.
/// </remarks>
public static class SceneLibrary
{
    /// <summary>Breite, auf die die Bilder dekodiert werden (entspricht der Originalbreite).</summary>
    public const int DecodeWidth = 960;

    private static readonly Lazy<ImageSource> LanternImage = new(() => Load("laterne.jpg"));
    private static readonly Lazy<ImageSource> WardrobeImage = new(() => Load("schrank.jpg"));
    private static readonly Lazy<ImageSource> SledgeImage = new(() => Load("schlitten.jpg"));
    private static readonly Lazy<ImageSource> ThawImage = new(() => Load("tauwetter.jpg"));
    private static readonly Lazy<ImageSource> CoronationImage = new(() => Load("cair-paravel.jpg"));
    private static readonly Lazy<ImageSource> StoneCourtyardImage = new(() => Load("steinhof.jpg"));

    /// <summary>Der Laternenpfahl im verschneiten Wald (die Reise im Licht).</summary>
    public static ImageSource Lantern => LanternImage.Value;

    /// <summary>Der offene Kleiderschrank (bereit zur Wacht, Rückkehr nach einem Abbruch).</summary>
    public static ImageSource Wardrobe => WardrobeImage.Value;

    /// <summary>Der Schlitten der Königin (Ablenkung, "Türkischer Honig").</summary>
    public static ImageSource Sledge => SledgeImage.Value;

    /// <summary>Das Tauwetter: Der Schlitten bleibt im Matsch stecken (Ende im Tauwetter).</summary>
    public static ImageSource Thaw => ThawImage.Value;

    /// <summary>Die Krönung in Cair Paravel (Ende im Frühling).</summary>
    public static ImageSource Coronation => CoronationImage.Value;

    /// <summary>Der Hof der Steinfiguren mit dem ersten goldenen Schein am Tor (Ende im Winter).</summary>
    public static ImageSource StoneCourtyard => StoneCourtyardImage.Value;

    private static BitmapImage Load(string fileName)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri("pack://application:,,,/Assets/Szenen/" + fileName, UriKind.Absolute);
        image.DecodePixelWidth = DecodeWidth;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }
}
