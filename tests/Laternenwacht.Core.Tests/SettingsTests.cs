using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.Core.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly TempDirectory _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Defaults_are_valid() => Assert.Empty(SettingsValidator.Validate(FocusSettings.Default));

    [Theory]
    [InlineData(0)]
    [InlineData(481)]
    public void Duration_out_of_range_is_rejected(int minutes) =>
        Assert.NotEmpty(SettingsValidator.Validate(FocusSettings.Default with { DefaultDuration = TimeSpan.FromMinutes(minutes) }));

    [Fact]
    public void Overlapping_lists_are_rejected() =>
        Assert.NotEmpty(SettingsValidator.Validate(FocusSettings.Default with { AllowedProcesses = ["discord"], DistractingProcesses = ["discord"] }));

    [Fact]
    public void Allow_list_mode_requires_companions() =>
        Assert.NotEmpty(SettingsValidator.Validate(FocusSettings.Default with { Mode = ClassificationMode.AllowList, AllowedProcesses = [] }));

    [Theory]
    [InlineData("Discord.EXE", "discord")]
    [InlineData("  code  ", "code")]
    [InlineData("battle.net", "battle.net")]
    public void Process_names_are_normalized(string raw, string expected) =>
        Assert.Equal(expected, ProcessNames.Normalize(raw));

    [Theory]
    [InlineData(@"C:\Windows\evil")]
    [InlineData("../../etc")]
    [InlineData("name\u0000with-null")]
    [InlineData("")]
    public void Suspicious_process_names_are_invalid(string raw) =>
        Assert.False(ProcessNames.IsValid(ProcessNames.Normalize(raw)));

    [Fact]
    public void List_parsing_splits_deduplicates_and_reports_invalid()
    {
        var (valid, invalid) = ProcessNames.ParseList("Discord, steam.exe;discord\nC:\\bad");

        Assert.Equal(["discord", "steam"], valid);
        Assert.Equal([@"C:\bad"], invalid);
    }

    [Fact]
    public void Settings_roundtrip_through_store()
    {
        var store = new SettingsStore(_dir.File("einstellungen.json"));
        var settings = FocusSettings.Default with { DefaultDuration = TimeSpan.FromMinutes(50), Mode = ClassificationMode.AllowList };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(settings.DefaultDuration, loaded.DefaultDuration);
        Assert.Equal(settings.Mode, loaded.Mode);
        Assert.Equal(settings.AllowedProcesses, loaded.AllowedProcesses);
        Assert.Null(store.LastLoadWarning);
    }

    [Theory]
    [InlineData("{ kaputt")]
    [InlineData("{\"defaultDuration\":\"99:00:00\"}")]
    [InlineData("{\"unbekanntesFeld\":1}")]
    [InlineData("{\"allowedProcesses\":[\"C:\\\\evil.exe\"]}")]
    public void Corrupt_or_invalid_files_fall_back_to_defaults(string json)
    {
        File.WriteAllText(_dir.File("einstellungen.json"), json);
        var store = new SettingsStore(_dir.File("einstellungen.json"));

        var loaded = store.Load();

        Assert.Equal(FocusSettings.Default.DefaultDuration, loaded.DefaultDuration);
        Assert.NotNull(store.LastLoadWarning);
    }

    [Fact]
    public void Invalid_settings_are_never_saved()
    {
        var store = new SettingsStore(_dir.File("einstellungen.json"));

        Assert.Throws<ArgumentException>(() => store.Save(FocusSettings.Default with { IdleThreshold = TimeSpan.Zero }));
        Assert.False(File.Exists(store.FilePath));
    }
}
