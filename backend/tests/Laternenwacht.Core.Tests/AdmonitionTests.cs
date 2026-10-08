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
    public void Twenty_pieces_of_turkish_delight_are_answered_by_peter()
    {
        var shown = new HashSet<string> { "count:3", "count:5", "count:10", "count:15" };

        var admonition = Admonitions.Next(20, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe);

        Assert.NotNull(admonition);
        Assert.Equal("count:20", admonition!.Key);
        Assert.Contains("trockenes Brot", admonition.Text, StringComparison.Ordinal);
        Assert.Equal(ChronicleBook.LionWitchWardrobe, admonition.Book);
        Assert.Equal(new Encouragement("Genau so ging es Edmund. Was sie verspricht, hält sie nicht.", "Peter",
            ChronicleBook.LionWitchWardrobe), admonition.Reply);
    }

    [Fact]
    public void The_queen_never_has_the_last_word_in_the_lion_witch_and_wardrobe()
    {
        var shown = new HashSet<string>();
        var sayings = Admonitions.For(ChronicleBook.LionWitchWardrobe);

        for (var i = 0; i < Admonitions.CountThresholds.Count; i++)
        {
            var count = Admonitions.CountThresholds[i];
            var admonition = Admonitions.Next(count, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe);

            // Genau ein Spruch je Schwelle – und jeder Mahnruf der Königin bekommt eine Antwort aus Narnia.
            Assert.NotNull(admonition);
            Assert.Equal($"count:{count}", admonition!.Key);
            Assert.Equal(sayings[i], admonition.Text);
            Assert.NotNull(admonition.Reply);
            Assert.Equal(ChronicleBook.LionWitchWardrobe, admonition.Reply!.Book);
            Assert.False(string.IsNullOrWhiteSpace(admonition.Reply.Text));
            Assert.False(string.IsNullOrWhiteSpace(admonition.Reply.Speaker));
            Assert.NotEqual(admonition.Text, admonition.Reply.Text);
            Assert.Null(Admonitions.Next(count, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe));
        }
    }

    [Fact]
    public void Replies_to_the_queen_are_all_different()
    {
        var shown = new HashSet<string>();
        var replies = Admonitions.CountThresholds
            .Select(c => Admonitions.Next(c, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe)!.Reply!.Text)
            .ToList();

        Assert.Equal(replies.Count, replies.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Aslan_answers_only_at_fifty()
    {
        var fifty = Admonitions.Next(50, TimeSpan.Zero, new HashSet<string>(), ChronicleBook.LionWitchWardrobe);

        Assert.Equal("Aslan", fifty!.Reply!.Speaker);

        var shown = new HashSet<string>();
        var others = Admonitions.CountThresholds.Take(6)
            .Select(c => Admonitions.Next(c, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe)!.Reply!.Speaker);
        Assert.DoesNotContain("Aslan", others);
    }

    [Fact]
    public void Frost_admonitions_come_without_a_reply()
    {
        var shown = new HashSet<string>();

        foreach (var minutes in Admonitions.FrostMinuteThresholds)
        {
            var admonition = Admonitions.Next(0, TimeSpan.FromMinutes(minutes), shown, ChronicleBook.LionWitchWardrobe);

            Assert.Equal($"frost:{minutes}", admonition!.Key);
            Assert.Null(admonition.Reply);
        }
    }

    [Theory]
    [MemberData(nameof(Volumes))]
    public void Only_the_lion_witch_and_wardrobe_carries_replies(ChronicleBook book)
    {
        var shown = new HashSet<string>();
        var admonitions = Admonitions.CountThresholds
            .Select(c => Admonitions.Next(c, TimeSpan.Zero, shown, book)!)
            .Concat(Admonitions.FrostMinuteThresholds
                .Select(m => Admonitions.Next(0, TimeSpan.FromMinutes(m), shown, book)!))
            .ToList();

        Assert.Equal(Admonitions.CountThresholds.Count + Admonitions.FrostMinuteThresholds.Count, admonitions.Count);
        Assert.Equal(book == ChronicleBook.LionWitchWardrobe ? Admonitions.CountThresholds.Count : 0,
            admonitions.Count(a => a.Reply is not null));
    }

    [Fact]
    public void Mixed_mode_gives_the_queen_a_reply_whenever_her_book_is_drawn()
    {
        for (var seed = 0; seed < ChronicleBooks.Volumes.Count; seed++)
        {
            var shown = new HashSet<string>();
            foreach (var count in Admonitions.CountThresholds)
            {
                var admonition = Admonitions.Next(count, TimeSpan.Zero, shown, ChronicleBook.All, seed)!;
                Assert.Equal(admonition.Book == ChronicleBook.LionWitchWardrobe, admonition.Reply is not null);
            }
        }
    }

    [Fact]
    public void Queen_signature_is_set() =>
        Assert.StartsWith("Jadis", Admonitions.QueenSignature, StringComparison.Ordinal);

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
