using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class FormattingTests
{
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(65, "01:05")]
    [InlineData(3600, "1:00:00")]
    [InlineData(-5, "00:00")]
    public void Clock_formats_seconds(int seconds, string expected) =>
        Assert.Equal(expected, TimeFormat.Clock(TimeSpan.FromSeconds(seconds)));

    [Theory]
    [InlineData(0.0, RealmMood.Spring)]
    [InlineData(0.099, RealmMood.Spring)]
    [InlineData(0.10, RealmMood.Thaw)]
    [InlineData(0.25, RealmMood.Winter)]
    [InlineData(1.0, RealmMood.Winter)]
    public void Mood_follows_frost_ratio(double ratio, RealmMood expected) =>
        Assert.Equal(expected, RealmMoods.FromFrost(ratio));
}
