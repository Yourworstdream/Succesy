using Laternenwacht.Core.Media;

namespace Laternenwacht.Core.Tests;

public sealed class MemeCatalogTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    private void Create(string name, int bytes = 16) => File.WriteAllBytes(_dir.File(name), new byte[bytes]);

    [Fact]
    public void Missing_directory_yields_empty_list() =>
        Assert.Empty(MemeCatalog.Scan(_dir.File("gibt-es-nicht")));

    [Fact]
    public void Only_image_files_are_listed_sorted()
    {
        Create("b-katze.PNG");
        Create("a-ophelia.jpg");
        Create("notiz.txt");
        Create("boese.exe");
        Create("bild.jpg.lnk");

        var files = MemeCatalog.Scan(_dir.Path).Select(Path.GetFileName);

        Assert.Equal(["a-ophelia.jpg", "b-katze.PNG"], files);
    }

    [Fact]
    public void Empty_files_are_skipped()
    {
        Create("leer.jpg", bytes: 0);

        Assert.Empty(MemeCatalog.Scan(_dir.Path));
    }

    [Fact]
    public void Subdirectories_are_not_searched()
    {
        Directory.CreateDirectory(_dir.File("unterordner"));
        File.WriteAllBytes(Path.Combine(_dir.File("unterordner"), "versteckt.jpg"), new byte[16]);

        Assert.Empty(MemeCatalog.Scan(_dir.Path));
    }

    [Fact]
    public void Number_of_files_is_limited()
    {
        for (var i = 0; i < MemeCatalog.MaxFiles + 5; i++)
        {
            Create($"meme{i:000}.png");
        }

        Assert.Equal(MemeCatalog.MaxFiles, MemeCatalog.Scan(_dir.Path).Count);
    }
}
