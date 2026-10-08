using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

/// <summary>
/// Alle Sprüche sind eigene Formulierungen. Diese Tests halten bekannte buchnahe Wendungen fern,
/// die bei der Durchsicht ersetzt wurden, und prüfen die Stimmen von Band II.
/// </summary>
public class OwnWordingTests
{
    private static readonly string[] BookPhrases =
    [
        "weiter hinauf und weiter hinein",
        "immer winter, nie weihnachten",
        "der löwe sei unterwegs",
        "steintisch bebt",
    ];

    private static IEnumerable<string> AllTexts()
    {
        foreach (var book in ChronicleBooks.Volumes)
        {
            foreach (var saying in Admonitions.For(book))
            {
                yield return saying;
            }

            foreach (var absence in new[] { TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30) })
            {
                yield return Homecomings.For(book, absence).Text;
            }

            for (var i = 0; i < Praises.StreakMilestones.Count; i++)
            {
                yield return Praises.ForStreak(book, i).Text;
            }
        }

        var shown = new HashSet<string>();
        foreach (var count in Admonitions.CountThresholds)
        {
            yield return Admonitions.Next(count, TimeSpan.Zero, shown, ChronicleBook.LionWitchWardrobe)!.Reply!.Text;
        }

        foreach (var station in Journey.Stations)
        {
            yield return station.Narration;
        }

        foreach (var ending in Enum.GetValues<JourneyEnding>())
        {
            yield return Journey.Describe(ending).Narration;
        }

        foreach (var chapter in Enum.GetValues<TemptationChapter>())
        {
            yield return Journey.TemptationLine(chapter, "steam");
        }
    }

    [Fact]
    public void No_text_repeats_a_replaced_book_phrase()
    {
        var texts = AllTexts().ToList();

        Assert.True(texts.Count > 100);
        foreach (var text in texts)
        {
            foreach (var phrase in BookPhrases)
            {
                Assert.DoesNotContain(phrase, text.ToLowerInvariant(), StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Lion_witch_and_wardrobe_welcomes_without_reproach()
    {
        var brief = Homecomings.For(ChronicleBook.LionWitchWardrobe, TimeSpan.FromSeconds(30));
        var moderate = Homecomings.For(ChronicleBook.LionWitchWardrobe, TimeSpan.FromMinutes(5));
        var extended = Homecomings.For(ChronicleBook.LionWitchWardrobe, TimeSpan.FromMinutes(15));

        Assert.Equal("Lucy", brief.Speaker);
        Assert.Equal("Frau Biber", moderate.Speaker);
        Assert.Equal("Peter", extended.Speaker);
        Assert.StartsWith("Edmund kam auch zurück", extended.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Lion_witch_and_wardrobe_praise_keeps_its_speakers()
    {
        var speakers = Enumerable.Range(0, Praises.StreakMilestones.Count)
            .Select(i => Praises.ForStreak(ChronicleBook.LionWitchWardrobe, i).Speaker);

        Assert.Equal(["Lucy", "Herr Tumnus", "Herr Biber", "der Weihnachtsmann", "Peter"], speakers);
    }
}
