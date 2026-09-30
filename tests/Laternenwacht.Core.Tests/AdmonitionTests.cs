using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class AdmonitionTests
{
    [Fact]
    public void Nothing_below_first_threshold() =>
        Assert.Null(Admonitions.Next(2, TimeSpan.FromMinutes(1), new HashSet<string>()));

    [Fact]
    public void Twenty_distractions_summon_the_ruler_of_cair_paravel()
    {
        var shown = new HashSet<string> { "count:3", "count:5", "count:10", "count:15" };

        var admonition = Admonitions.Next(20, TimeSpan.Zero, shown);

        Assert.NotNull(admonition);
        Assert.Contains("Cair Paravel", admonition!.Text, StringComparison.Ordinal);
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
}
