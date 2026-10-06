using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class HomecomingTests
{
    [Theory]
    [InlineData(30, AbsenceLength.Brief)]
    [InlineData(119, AbsenceLength.Brief)]
    [InlineData(120, AbsenceLength.Moderate)]
    [InlineData(599, AbsenceLength.Moderate)]
    [InlineData(600, AbsenceLength.Extended)]
    public void Absence_is_classified(int seconds, AbsenceLength expected) =>
        Assert.Equal(expected, Homecomings.Classify(TimeSpan.FromSeconds(seconds)));

    [Fact]
    public void Long_absence_on_the_dawn_treader_is_greeted_by_reepicheep()
    {
        var greeting = Homecomings.For(ChronicleBook.DawnTreader, TimeSpan.FromMinutes(15));

        Assert.Equal("Riepiepich", greeting.Speaker);
        Assert.StartsWith("Kaspian hat lange auf dich gewartet.", greeting.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_book_has_a_greeting_for_every_length()
    {
        foreach (var book in ChronicleBooks.Volumes)
        {
            var texts = Enum.GetValues<AbsenceLength>()
                .Select(l => Homecomings.For(book, l switch
                {
                    AbsenceLength.Brief => TimeSpan.FromSeconds(30),
                    AbsenceLength.Moderate => TimeSpan.FromMinutes(5),
                    _ => TimeSpan.FromMinutes(30),
                }))
                .ToList();

            Assert.All(texts, h => Assert.Equal(book, h.Book));
            Assert.All(texts, h => Assert.False(string.IsNullOrWhiteSpace(h.Speaker)));
            Assert.Equal(3, texts.Select(h => h.Text).Distinct().Count());
        }
    }

    [Fact]
    public void Mixed_mode_picks_a_concrete_book() =>
        Assert.Contains(Homecomings.For(ChronicleBook.All, TimeSpan.FromMinutes(1), seed: -3).Book, ChronicleBooks.Volumes);
}
