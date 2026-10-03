using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class ShuffleBagTests
{
    [Fact]
    public void Empty_bag_yields_nothing() =>
        Assert.False(new ShuffleBag<string>([]).TryNext(out _));

    [Fact]
    public void Every_item_appears_once_per_round()
    {
        var bag = new ShuffleBag<int>(Enumerable.Range(1, 5), new Random(7));

        var round = Enumerable.Range(0, 5).Select(_ => bag.TryNext(out var x) ? x : -1).ToList();

        Assert.Equal([1, 2, 3, 4, 5], round.Order());
    }

    [Fact]
    public void Never_repeats_the_same_item_twice_in_a_row()
    {
        var bag = new ShuffleBag<int>(Enumerable.Range(1, 3), new Random(1));
        var previous = -1;

        for (var i = 0; i < 300; i++)
        {
            Assert.True(bag.TryNext(out var current));
            Assert.NotEqual(previous, current);
            previous = current;
        }
    }

    [Fact]
    public void Single_item_is_repeated()
    {
        var bag = new ShuffleBag<string>(["ophelia"]);

        Assert.True(bag.TryNext(out var a));
        Assert.True(bag.TryNext(out var b));
        Assert.Equal(a, b);
    }
}
