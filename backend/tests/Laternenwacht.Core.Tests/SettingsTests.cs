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

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(1e9, 0.0)]
    public void Invalid_bar_positions_are_rejected(double left, double top) =>
        Assert.NotEmpty(SettingsValidator.Validate(FocusSettings.Default with { BarLeft = left, BarTop = top }));

    [Fact]
    public void Half_a_bar_position_is_rejected() =>
        Assert.NotEmpty(SettingsValidator.Validate(FocusSettings.Default with { BarLeft = 100 }));

    [Fact]
    public void Book_notifications_and_position_roundtrip()
    {
        var store = new SettingsStore(_dir.File("einstellungen.json"));
        var settings = FocusSettings.Default with
        {
            SayingsBook = ChronicleBook.SilverChair,
            ShowNotifications = false,
            BarLeft = -1200.5,
            BarTop = 40,
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(ChronicleBook.SilverChair, loaded.SayingsBook);
        Assert.False(loaded.ShowNotifications);
        Assert.Equal(-1200.5, loaded.BarLeft);
        Assert.True(loaded.HasCustomBarPosition);
        Assert.DoesNotContain("hasCustomBarPosition", File.ReadAllText(store.FilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_fields_keep_their_defaults()
    {
        File.WriteAllText(_dir.File("einstellungen.json"), "{\"defaultDuration\":\"00:30:00\",\"mode\":\"BlockList\"}");
        var store = new SettingsStore(_dir.File("einstellungen.json"));

        var loaded = store.Load();

        Assert.Null(store.LastLoadWarning);
        Assert.Equal(TimeSpan.FromMinutes(30), loaded.DefaultDuration);
        Assert.Equal(ChronicleBook.All, loaded.SayingsBook);
        Assert.False(loaded.HasCustomBarPosition);
        Assert.True(loaded.ShowNotifications);
        Assert.True(loaded.ShowMemes);
        Assert.Equal(FocusSettings.Default.IdleThreshold, loaded.IdleThreshold);
    }

    [Fact]
    public void Invalid_settings_are_never_saved()
    {
        var store = new SettingsStore(_dir.File("einstellungen.json"));

        Assert.Throws<ArgumentException>(() => store.Save(FocusSettings.Default with { IdleThreshold = TimeSpan.Zero }));
        Assert.False(File.Exists(store.FilePath));
    }
}
