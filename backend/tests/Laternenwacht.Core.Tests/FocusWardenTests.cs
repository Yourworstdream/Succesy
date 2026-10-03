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
}
