using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class FocusWardenTests
{
    private readonly ManualTimeProvider _time = new();
    private readonly ScriptedProbe _probe = new();

    [Fact]
    public void Session_ended_is_raised_exactly_once_on_completion()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var ended = new List<SessionRecord>();
        warden.SessionEnded += (_, r) => ended.Add(r);

        warden.Start(TimeSpan.FromMinutes(1));
        for (var i = 0; i < 70; i++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            warden.Pulse();
        }

        warden.Abort();

        var record = Assert.Single(ended);
        Assert.Equal(SessionPhase.Completed, record.Outcome);
        Assert.False(warden.IsActive);
    }

    [Fact]
    public void Cannot_start_two_sessions()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        warden.Start(TimeSpan.FromMinutes(5));

        Assert.Throws<InvalidOperationException>(() => warden.Start(TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Applied_settings_take_effect_immediately()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        _probe.Next = new ActivitySnapshot("game", TimeSpan.Zero);
        var session = warden.Start(TimeSpan.FromMinutes(5));
        Assert.Equal(ActivityState.Focused, session.CurrentState);

        warden.ApplySettings(FocusSettings.Default with { DistractingProcesses = ["game"] });
        _time.Advance(TimeSpan.FromSeconds(1));
        warden.Pulse();

        Assert.Equal(ActivityState.Distracted, session.CurrentState);
    }

    [Fact]
    public void Distraction_started_fires_once_per_episode()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var episodes = new List<DistractionStarted>();
        warden.DistractionStarted += (_, e) => episodes.Add(e);
        warden.Start(TimeSpan.FromMinutes(10));

        void Tick(string process, int seconds)
        {
            _probe.Next = new ActivitySnapshot(process, TimeSpan.Zero);
            for (var i = 0; i < seconds; i++)
            {
                _time.Advance(TimeSpan.FromSeconds(1));
                warden.Pulse();
            }
        }

        Tick("discord", 5);   // Episode 1 – anhaltend, nur einmal gemeldet
        Tick("devenv", 3);
        Tick("steam", 2);     // Episode 2

        Assert.Equal([new DistractionStarted("discord", 1), new DistractionStarted("steam", 2)], episodes);
    }

    [Fact]
    public void Starting_while_distracted_reports_first_episode()
    {
        _probe.Next = new ActivitySnapshot("discord", TimeSpan.Zero);
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        DistractionStarted? reported = null;
        warden.DistractionStarted += (_, e) => reported = e;

        warden.Start(TimeSpan.FromMinutes(5));

        Assert.Equal(new DistractionStarted("discord", 1), reported);
    }

    [Fact]
    public void No_distraction_events_while_paused()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var count = 0;
        warden.DistractionStarted += (_, _) => count++;
        warden.Start(TimeSpan.FromMinutes(5));
        warden.Pause();

        _probe.Next = new ActivitySnapshot("discord", TimeSpan.Zero);
        _time.Advance(TimeSpan.FromSeconds(1));
        warden.Pulse();

        Assert.Equal(0, count);
    }

    private void Run(FocusWarden warden, string process, int seconds)
    {
        _probe.Next = new ActivitySnapshot(process, TimeSpan.Zero);
        for (var i = 0; i < seconds; i++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            warden.Pulse();
        }
    }

    [Fact]
    public void Returning_to_work_is_reported_with_absence_and_process()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var returns = new List<ReturnedToWork>();
        warden.ReturnedToWork += (_, e) => returns.Add(e);
        warden.Start(TimeSpan.FromMinutes(30));

        Run(warden, "devenv", 5);
        Run(warden, "hearthstone", 120);   // Ablenkung beginnt beim ersten Puls, 119 s + 1 s beim Wechsel
        Run(warden, "devenv", 3);

        var back = Assert.Single(returns);
        Assert.Equal("hearthstone", back.ProcessName);
        Assert.Equal(TimeSpan.FromSeconds(120), back.Absence);
        Assert.Equal(1, back.Episode);
    }

    [Fact]
    public void Short_detours_are_not_welcomed()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var count = 0;
        warden.ReturnedToWork += (_, _) => count++;
        warden.Start(TimeSpan.FromMinutes(30));

        Run(warden, "discord", 5);
        Run(warden, "devenv", 3);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Each_episode_measures_its_own_absence()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var returns = new List<ReturnedToWork>();
        warden.ReturnedToWork += (_, e) => returns.Add(e);
        warden.Start(TimeSpan.FromMinutes(30));

        Run(warden, "devenv", 2);
        Run(warden, "steam", 30);
        Run(warden, "devenv", 2);
        Run(warden, "discord", 15);
        Run(warden, "devenv", 2);

        Assert.Equal([TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(15)], returns.Select(r => r.Absence));
        Assert.Equal([1, 2], returns.Select(r => r.Episode));
    }

    [Fact]
    public void Drifting_into_idle_is_not_a_return_to_work()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var count = 0;
        warden.ReturnedToWork += (_, _) => count++;
        warden.Start(TimeSpan.FromMinutes(30));

        Run(warden, "discord", 20);
        _probe.Next = new ActivitySnapshot("discord", TimeSpan.FromMinutes(5));
        _time.Advance(TimeSpan.FromSeconds(1));
        warden.Pulse();

        Assert.Equal(0, count);
    }

    [Fact]
    public void Streak_milestones_are_praised_once_per_streak()
    {
        var warden = new FocusWarden(_probe, _time, FocusSettings.Default, "laternenwacht");
        var praised = new List<int>();
        warden.FocusStreakReached += (_, e) => praised.Add(e.Minutes);
        warden.Start(TimeSpan.FromHours(2));

        Run(warden, "devenv", 26 * 60);      // 10 und 25 Minuten
        Run(warden, "discord", 20);           // Serie bricht
        Run(warden, "devenv", 11 * 60);      // neue Serie: wieder 10 Minuten

        Assert.Equal([10, 25, 10], praised);
    }

    [Fact]
    public void Old_journal_records_without_streak_still_load()
    {
        const string oldPayload = "{\"id\":\"6f9619ff-8b86-d011-b42d-00c04fc964ff\",\"startedAtUtc\":\"2026-09-30T08:00:00+00:00\",\"endedAtUtc\":\"2026-09-30T08:25:00+00:00\",\"planned\":\"00:25:00\",\"focused\":\"00:25:00\",\"distracted\":\"00:00:00\",\"away\":\"00:00:00\",\"distractionCount\":0,\"outcome\":\"Completed\",\"topDistractions\":[]}";

        var record = System.Text.Json.JsonSerializer.Deserialize(oldPayload, Laternenwacht.Core.Integrity.CoreJsonContext.Default.SessionRecord);

        Assert.NotNull(record);
        Assert.Equal(TimeSpan.Zero, record!.LongestFocusStreak);
    }
}
