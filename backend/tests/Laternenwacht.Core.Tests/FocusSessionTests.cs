using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class FocusSessionTests
{
    private readonly ManualTimeProvider _time = new();

    private FocusSession NewSession(int minutes = 25, ActivityState initial = ActivityState.Focused) =>
        new(TimeSpan.FromMinutes(minutes), _time, initial, "devenv");

    private void Tick(FocusSession session, ActivityState state, string? process = "devenv", int seconds = 1)
    {
        for (var i = 0; i < seconds; i++)
        {
            _time.Advance(TimeSpan.FromSeconds(1));
            session.Update(state, process);
        }
    }

    [Fact]
    public void Time_is_credited_to_the_previous_state()
    {
        var session = NewSession();

        Tick(session, ActivityState.Distracted, "discord", seconds: 10);   // 1 s Fokus, danach 9 s Frost
        Tick(session, ActivityState.Focused, seconds: 5);                   // 1 s Frost, danach 4 s Fokus

        Assert.Equal(TimeSpan.FromSeconds(5), session.Focused);
        Assert.Equal(TimeSpan.FromSeconds(10), session.Distracted);
        Assert.Equal(1, session.DistractionCount);
    }

    [Fact]
    public void Each_new_distraction_episode_is_counted()
    {
        var session = NewSession();

        Tick(session, ActivityState.Distracted, "discord", 3);
        Tick(session, ActivityState.Focused, seconds: 3);
        Tick(session, ActivityState.Distracted, "steam", 3);

        Assert.Equal(2, session.DistractionCount);
    }

    [Fact]
    public void Starting_distracted_counts_as_first_episode()
    {
        var session = NewSession(initial: ActivityState.Distracted);

        Assert.Equal(1, session.DistractionCount);
    }

    [Fact]
    public void Session_completes_exactly_at_planned_duration()
    {
        var session = NewSession(minutes: 1);

        Tick(session, ActivityState.Focused, seconds: 59);
        Assert.Equal(SessionPhase.Running, session.Phase);

        _time.Advance(TimeSpan.FromSeconds(5));
        session.Update(ActivityState.Focused, "devenv");

        Assert.Equal(SessionPhase.Completed, session.Phase);
        Assert.Equal(TimeSpan.FromMinutes(1), session.Measured);
        Assert.Equal(TimeSpan.Zero, session.Remaining);
    }

    [Fact]
    public void Paused_time_is_not_measured()
    {
        var session = NewSession();
        Tick(session, ActivityState.Focused, seconds: 10);

        session.Pause();
        _time.Advance(TimeSpan.FromMinutes(10));
        session.Update(ActivityState.Distracted, "discord");  // wird ignoriert
        session.Resume();
        Tick(session, ActivityState.Focused, seconds: 5);

        Assert.Equal(TimeSpan.FromSeconds(15), session.Measured);
        Assert.Equal(TimeSpan.Zero, session.Distracted);
    }

    [Fact]
    public void Large_gaps_such_as_standby_count_as_away()
    {
        var session = NewSession();

        _time.Advance(TimeSpan.FromMinutes(5));
        session.Update(ActivityState.Focused, "devenv");

        Assert.Equal(TimeSpan.FromMinutes(5), session.Away);
        Assert.Equal(TimeSpan.Zero, session.Focused);
    }

    [Fact]
    public void Changing_the_wall_clock_does_not_affect_measurement()
    {
        var session = NewSession();

        _time.TamperWallClock(TimeSpan.FromHours(3));
        Tick(session, ActivityState.Focused, seconds: 10);

        Assert.Equal(TimeSpan.FromSeconds(10), session.Measured);
    }

    [Fact]
    public void Abort_produces_record_with_aborted_outcome()
    {
        var session = NewSession();
        Tick(session, ActivityState.Distracted, "discord", 4);

        session.Abort();
        var record = session.ToRecord();

        Assert.Equal(SessionPhase.Aborted, record.Outcome);
        Assert.Equal(TimeSpan.FromSeconds(4), record.Measured);
        Assert.Equal("discord", Assert.Single(record.TopDistractions).ProcessName);
    }

    [Fact]
    public void Record_of_running_session_is_rejected()
    {
        var session = NewSession();

        Assert.Throws<InvalidOperationException>(() => session.ToRecord());
    }

    [Fact]
    public void Top_distractions_are_ordered_by_duration()
    {
        var session = NewSession();
        Tick(session, ActivityState.Distracted, "steam", 3);
        Tick(session, ActivityState.Distracted, "discord", 6);
        Tick(session, ActivityState.Focused, seconds: 1);

        var top = session.TopDistractions(5);

        Assert.Equal(["discord", "steam"], top.Select(t => t.ProcessName));
    }
}
