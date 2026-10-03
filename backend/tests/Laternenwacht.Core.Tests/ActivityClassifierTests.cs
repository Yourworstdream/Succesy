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

public class KnownDistractionTests
{
    private static ActivityState Classify(FocusSettings settings, string process) =>
        new ActivityClassifier(settings, "laternenwacht").Classify(new ActivitySnapshot(process, TimeSpan.Zero));

    [Theory]
    [InlineData("Hearthstone")]
    [InlineData("Battle.net")]
    [InlineData("League of Legends")]
    [InlineData("Discord")]
    public void Well_known_distractions_are_detected_by_default(string process) =>
        Assert.Equal(ActivityState.Distracted, Classify(FocusSettings.Default with { DistractingProcesses = [] }, process));

    [Fact]
    public void Companions_override_the_known_catalogue() =>
        Assert.Equal(ActivityState.Focused, Classify(FocusSettings.Default with { AllowedProcesses = ["discord"] }, "Discord"));

    [Fact]
    public void Catalogue_can_be_switched_off() =>
        Assert.Equal(ActivityState.Focused, Classify(FocusSettings.Default with { UseKnownDistractions = false, DistractingProcesses = [] }, "Hearthstone"));

    [Fact]
    public void Catalogue_names_are_normalized() =>
        Assert.All(KnownDistractions.Names, n => Assert.Equal(ProcessNames.Normalize(n), n));

    [Fact]
    public void Marking_adds_to_distractions_and_removes_from_companions()
    {
        var settings = FocusSettings.Default with { AllowedProcesses = ["devenv", "hearthstone"] };

        var updated = SettingsEditing.MarkAsDistraction(settings, "Hearthstone.exe");

        Assert.NotNull(updated);
        Assert.Contains("hearthstone", updated!.DistractingProcesses);
        Assert.DoesNotContain("hearthstone", updated.AllowedProcesses);
        Assert.Empty(SettingsValidator.Validate(updated));
    }

    [Fact]
    public void Marking_twice_does_not_duplicate()
    {
        var once = SettingsEditing.MarkAsDistraction(FocusSettings.Default, "hearthstone")!;
        var twice = SettingsEditing.MarkAsDistraction(once, "HEARTHSTONE")!;

        Assert.Single(twice.DistractingProcesses, p => p == "hearthstone");
    }

    [Fact]
    public void Invalid_names_are_not_marked() =>
        Assert.Null(SettingsEditing.MarkAsDistraction(FocusSettings.Default, @"C:\evil"));
}
