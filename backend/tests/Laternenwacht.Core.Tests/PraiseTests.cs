using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class PraiseTests
{
    [Fact]
    public void Every_book_praises_every_milestone_with_distinct_lines()
    {
        foreach (var book in ChronicleBooks.Volumes)
        {
            var lines = Enumerable.Range(0, Praises.StreakMilestones.Count).Select(i => Praises.ForStreak(book, i)).ToList();

            Assert.All(lines, l => Assert.Equal(book, l.Book));
            Assert.All(lines, l => Assert.False(string.IsNullOrWhiteSpace(l.Speaker)));
            Assert.Equal(lines.Count, lines.Select(l => l.Text).Distinct().Count());
        }
    }

    [Fact]
    public void Reepicheep_salutes_after_45_minutes_in_prince_caspian()
    {
        var praise = Praises.ForStreak(ChronicleBook.PrinceCaspian, 2);

        Assert.Equal("Riepiepich", praise.Speaker);
        Assert.Equal(45, Praises.StreakMilestones[2]);
    }

    [Fact]
    public void Invalid_milestone_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Praises.ForStreak(ChronicleBook.DawnTreader, 99));
}
