using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class ActivityClassifierTests
{
    private static readonly FocusSettings Settings = new()
    {
        AllowedProcesses = ["devenv", "code"],
        DistractingProcesses = ["discord"],
        IdleThreshold = TimeSpan.FromMinutes(2),
    };

    private static ActivityState Classify(FocusSettings settings, string? process, int idleSeconds = 0) =>
        new ActivityClassifier(settings, "Laternenwacht").Classify(new ActivitySnapshot(process, TimeSpan.FromSeconds(idleSeconds)));

    [Theory]
    [InlineData(ClassificationMode.BlockList, "discord", ActivityState.Distracted)]
    [InlineData(ClassificationMode.BlockList, "Discord.exe", ActivityState.Distracted)]
    [InlineData(ClassificationMode.BlockList, "unbekannt", ActivityState.Focused)]
    [InlineData(ClassificationMode.AllowList, "unbekannt", ActivityState.Distracted)]
    [InlineData(ClassificationMode.AllowList, "DEVENV", ActivityState.Focused)]
    [InlineData(ClassificationMode.AllowList, "laternenwacht", ActivityState.Focused)]
    public void Classifies_by_mode(ClassificationMode mode, string process, ActivityState expected) =>
        Assert.Equal(expected, Classify(Settings with { Mode = mode }, process));

    [Fact]
    public void Idle_beyond_threshold_is_away_even_in_distracting_app() =>
        Assert.Equal(ActivityState.Away, Classify(Settings, "discord", idleSeconds: 120));

    [Fact]
    public void Unknown_foreground_is_not_punished() =>
        Assert.Equal(ActivityState.Focused, Classify(Settings with { Mode = ClassificationMode.AllowList }, null));
}
