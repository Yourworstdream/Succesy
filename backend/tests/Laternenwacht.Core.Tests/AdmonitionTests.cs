using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class AdmonitionTests
{
    private const int SayingsPerBook = 11;

    public static TheoryData<ChronicleBook> Volumes()
    {
        var data = new TheoryData<ChronicleBook>();
        foreach (var book in ChronicleBooks.Volumes)
        {
            data.Add(book);
        }

        return data;
    }

    [Fact]
    public void Nothing_below_first_threshold() =>
        Assert.Null(Admonitions.Next(2, TimeSpan.FromMinutes(1), new HashSet<string>()));

    [Fact]
    public void Twenty_distractions_summon_the_ruler_of_cair_paravel()
    {
        var shown = new HashSet<string> { "count:3", "count:5", "count:10", "count:15" };

        var admonition = Admonitions.Next(20, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe);

        Assert.NotNull(admonition);
        Assert.Contains("Cair Paravel", admonition!.Text, StringComparison.Ordinal);
        Assert.Equal(ChronicleBook.LionWitchWardrobe, admonition.Book);
    }

    [Fact]
    public void Each_admonition_appears_only_once()
    {
        var shown = new HashSet<string>();

        Assert.NotNull(Admonitions.Next(3, TimeSpan.Zero, shown));
        Assert.Null(Admonitions.Next(3, TimeSpan.Zero, shown));
        Assert.Null(Admonitions.Next(4, TimeSpan.Zero, shown));
    }

    [Fact]
    public void Skipped_thresholds_are_not_replayed()
    {
        var shown = new HashSet<string>();

        var first = Admonitions.Next(12, TimeSpan.Zero, shown);

        Assert.Equal("count:10", first!.Key);
        Assert.Null(Admonitions.Next(12, TimeSpan.Zero, shown));
    }

    [Fact]
    public void Frost_minutes_trigger_their_own_admonitions() =>
        Assert.Equal("frost:10", Admonitions.Next(1, TimeSpan.FromMinutes(11), new HashSet<string>())!.Key);

    [Theory]
    [MemberData(nameof(Volumes))]
    public void Every_book_has_a_full_set_of_distinct_sayings(ChronicleBook book)
    {
        var sayings = Admonitions.For(book);

        Assert.Equal(SayingsPerBook, sayings.Count);
        Assert.Equal(SayingsPerBook, sayings.Distinct(StringComparer.Ordinal).Count());
        Assert.All(sayings, s => Assert.False(string.IsNullOrWhiteSpace(s)));
    }

    [Theory]
    [MemberData(nameof(Volumes))]
    public void Selected_book_is_used(ChronicleBook book)
    {
        var admonition = Admonitions.Next(5, TimeSpan.Zero, new HashSet<string>(), book);

        Assert.Equal(book, admonition!.Book);
        Assert.Equal(Admonitions.For(book)[1], admonition.Text);
    }

    [Fact]
    public void Mixed_mode_draws_from_several_books()
    {
        var shown = new HashSet<string>();
        var books = new HashSet<ChronicleBook>();

        foreach (var count in Admonitions.CountThresholds)
        {
            books.Add(Admonitions.Next(count, TimeSpan.Zero, shown, ChronicleBook.All, seed: 42)!.Book);
        }

        Assert.DoesNotContain(ChronicleBook.All, books);
        Assert.True(books.Count >= 5);
    }

    [Fact]
    public void Mixed_mode_is_deterministic_for_a_seed()
    {
        var a = Admonitions.Next(3, TimeSpan.Zero, new HashSet<string>(), ChronicleBook.All, seed: -7);
        var b = Admonitions.Next(3, TimeSpan.Zero, new HashSet<string>(), ChronicleBook.All, seed: -7);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Every_book_has_a_german_title() =>
        Assert.Equal(8, Enum.GetValues<ChronicleBook>().Select(ChronicleBooks.Title).Distinct().Count());
}
