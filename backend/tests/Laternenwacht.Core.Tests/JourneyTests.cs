using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.Core.Tests;

public class JourneyTests
{
    private static readonly TimeSpan Plan = TimeSpan.FromMinutes(15);   // 900 s → alle 100 s eine Station

    private static TimeSpan Seconds(double s) => TimeSpan.FromSeconds(s);

    private static SessionRecord Record(SessionPhase outcome, int plannedMinutes, double measuredSeconds,
        double distractedSeconds, int distractionCount) => new()
    {
        Id = Guid.NewGuid(),
        StartedAtUtc = new DateTimeOffset(2026, 10, 7, 8, 0, 0, TimeSpan.Zero),
        EndedAtUtc = new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.Zero),
        Planned = TimeSpan.FromMinutes(plannedMinutes),
        Focused = Seconds(measuredSeconds - distractedSeconds),
        Distracted = Seconds(distractedSeconds),
        Away = TimeSpan.Zero,
        DistractionCount = distractionCount,
        Outcome = outcome,
    };

    // ---- Stationen ---------------------------------------------------------------------------------

    [Fact]
    public void There_are_ten_stations_in_book_order()
    {
        Assert.Equal(10, Journey.StationCount);
        Assert.Equal(Journey.StationCount, Journey.Stations.Count);
        Assert.Equal(Enumerable.Range(1, 10), Journey.Stations.Select(s => s.Number));
        Assert.Equal(
            [
                "Der Laternenpfahl", "Der Schlitten der Königin", "Die leere Höhle", "Der Biberdamm",
                "Der Hof der Königin", "Das Tauwetter", "Der Steinerne Tisch", "Der Morgen am Tisch",
                "Die Schlacht", "Cair Paravel",
            ],
            Journey.Stations.Select(s => s.Name));
    }

    [Fact]
    public void Station_texts_are_distinct_short_and_free_of_placeholders()
    {
        Assert.Equal(10, Journey.Stations.Select(s => s.Narration).Distinct(StringComparer.Ordinal).Count());
        Assert.All(Journey.Stations, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Name));
            Assert.InRange(s.Narration.Length, 40, 180);
            Assert.DoesNotContain("{", s.Narration, StringComparison.Ordinal);
            Assert.DoesNotContain("Kap.", s.Narration, StringComparison.Ordinal);   // Kapitelnummern nie in der Oberfläche
        });
    }

    [Fact]
    public void Station_list_cannot_be_modified_from_outside() =>
        Assert.False(Journey.Stations is IList<JourneyStation> { IsReadOnly: false });

    [Fact]
    public void Book_order_keeps_the_corrected_sequence()
    {
        // Die Festrunde wird erst nach dem Weihnachtsmann versteinert; Lucy heilt Edmund erst nach Aslans Sieg.
        Assert.DoesNotContain("Festrunde", Journey.Stations[4].Narration, StringComparison.Ordinal);
        Assert.Contains("Festrunde", Journey.Stations[5].Narration, StringComparison.Ordinal);
        var battle = Journey.Stations[8].Narration;
        Assert.True(battle.IndexOf("besiegt", StringComparison.Ordinal) < battle.IndexOf("heilt", StringComparison.Ordinal));
    }

    // ---- Anteil und Station -------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 900, 0)]
    [InlineData(450, 900, 0.5)]
    [InlineData(900, 900, 1)]
    [InlineData(1200, 900, 1)]
    [InlineData(-5, 900, 0)]
    [InlineData(100, 0, 0)]
    [InlineData(100, -60, 0)]
    public void Fraction_is_clamped(double measured, double planned, double expected) =>
        Assert.Equal(expected, Journey.Fraction(Seconds(measured), Seconds(planned)), precision: 12);

    [Theory]
    [InlineData(0.0, 1)]       // 0 %
    [InlineData(0.111, 1)]     // 11,1 % liegt knapp vor 1/9
    [InlineData(0.112, 2)]
    [InlineData(0.5, 5)]
    [InlineData(0.889, 9)]
    [InlineData(0.999, 9)]     // 99,9 %: Cair Paravel noch nicht erreicht
    [InlineData(1.0, 10)]      // 100 %
    [InlineData(1.5, 10)]
    public void Station_follows_the_fraction_of_planned_time(double fraction, int expected) =>
        Assert.Equal(expected, Journey.StationNumber(Plan * fraction, Plan));

    [Theory]
    [InlineData(99.999, 1)]
    [InlineData(100, 2)]
    [InlineData(199.999, 2)]
    [InlineData(200, 3)]
    [InlineData(800, 9)]
    [InlineData(899.999, 9)]
    [InlineData(900, 10)]
    public void Stations_change_exactly_at_each_ninth(double measuredSeconds, int expected) =>
        Assert.Equal(expected, Journey.StationNumber(Seconds(measuredSeconds), Plan));

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(25)]
    [InlineData(61)]
    [InlineData(120)]
    public void Boundaries_are_exact_even_when_a_ninth_is_not_a_whole_tick(int plannedMinutes)
    {
        var planned = TimeSpan.FromMinutes(plannedMinutes).Ticks;

        for (var k = 1; k <= 9; k++)
        {
            var boundary = (planned * k + 8) / 9;   // erster Tick der Station k + 1
            Assert.Equal(k, Journey.StationNumber(TimeSpan.FromTicks(boundary - 1), TimeSpan.FromTicks(planned)));
            Assert.Equal(k + 1, Journey.StationNumber(TimeSpan.FromTicks(boundary), TimeSpan.FromTicks(planned)));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Without_planned_time_the_journey_stays_at_the_lamppost(int plannedSeconds)
    {
        Assert.Equal(1, Journey.StationNumber(Seconds(500), Seconds(plannedSeconds)));
        Assert.Equal("Der Laternenpfahl", Journey.StationAt(Seconds(500), Seconds(plannedSeconds)).Name);
        Assert.Equal(TimeSpan.Zero, Journey.UntilNextStation(Seconds(500), Seconds(plannedSeconds)));
    }

    [Fact]
    public void Negative_measured_time_counts_as_the_start() =>
        Assert.Equal(1, Journey.StationNumber(Seconds(-30), Plan));

    [Fact]
    public void Station_at_returns_the_matching_station()
    {
        for (var s = 0; s <= 900; s += 50)
        {
            var station = Journey.StationAt(Seconds(s), Plan);
            Assert.Equal(Journey.StationNumber(Seconds(s), Plan), station.Number);
            Assert.Same(Journey.Stations[station.Number - 1], station);
        }
    }

    [Theory]
    [InlineData(1, 0.0)]
    [InlineData(4, 1.0 / 3)]
    [InlineData(10, 1.0)]
    [InlineData(0, 0.0)]
    [InlineData(-3, 0.0)]
    [InlineData(11, 1.0)]
    public void Position_spaces_the_stations_evenly(int station, double expected) =>
        Assert.Equal(expected, Journey.PositionOf(station), precision: 12);

    [Fact]
    public void Position_of_the_current_station_never_runs_ahead_of_the_fraction()
    {
        for (var s = 0; s <= 900; s += 7)
        {
            var station = Journey.StationNumber(Seconds(s), Plan);
            Assert.True(Journey.PositionOf(station) <= Journey.Fraction(Seconds(s), Plan) + 1e-12);
        }
    }

    // ---- Nächste Station ----------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 99)]
    [InlineData(150, 50)]
    [InlineData(800, 100)]
    [InlineData(899, 1)]
    [InlineData(900, 0)]
    [InlineData(1000, 0)]
    [InlineData(-20, 100)]
    public void Time_until_next_station(double measuredSeconds, double expectedSeconds) =>
        Assert.Equal(Seconds(expectedSeconds), Journey.UntilNextStation(Seconds(measuredSeconds), Plan));

    [Theory]
    [InlineData(7)]
    [InlineData(25)]
    [InlineData(61)]
    public void Arriving_after_the_announced_time_reaches_the_next_station(int plannedMinutes)
    {
        var planned = TimeSpan.FromMinutes(plannedMinutes);
        var measured = TimeSpan.Zero;

        for (var expected = 1; expected < Journey.StationCount; expected++)
        {
            Assert.Equal(expected, Journey.StationNumber(measured, planned));
            var until = Journey.UntilNextStation(measured, planned);
            Assert.True(until > TimeSpan.Zero);
            Assert.Equal(expected, Journey.StationNumber(measured + until - TimeSpan.FromTicks(1), planned));
            measured += until;
        }

        Assert.Equal(planned, measured);
        Assert.Equal(Journey.StationCount, Journey.StationNumber(measured, planned));
        Assert.Equal(TimeSpan.Zero, Journey.UntilNextStation(measured, planned));
    }

    // ---- Verlockungen -------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, TemptationChapter.Sledge)]
    [InlineData(2, TemptationChapter.Sledge)]
    [InlineData(3, TemptationChapter.Sledge)]
    [InlineData(4, TemptationChapter.BeaverDam)]
    [InlineData(5, TemptationChapter.Castle)]
    [InlineData(7, TemptationChapter.Castle)]
    [InlineData(10, TemptationChapter.Castle)]
    [InlineData(0, TemptationChapter.Sledge)]
    [InlineData(11, TemptationChapter.Castle)]
    public void Temptation_chapter_follows_the_station(int station, TemptationChapter expected) =>
        Assert.Equal(expected, Journey.ChapterFor(station));

    [Fact]
    public void Temptation_lines_name_the_program_and_differ_by_chapter()
    {
        var lines = Enum.GetValues<TemptationChapter>().Select(c => Journey.TemptationLine(c, "Hearthstone")).ToList();

        Assert.All(lines, l => Assert.Contains("Hearthstone", l, StringComparison.Ordinal));
        Assert.All(lines, l => Assert.DoesNotContain("{", l, StringComparison.Ordinal));
        Assert.Equal(lines.Count, lines.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            "Ein Tropfen aus ihrer Flasche, und im Schnee liegt Türkischer Honig. Heute heißt er Hearthstone.",
            lines[0]);
        Assert.EndsWith("Was verspricht dir Hearthstone?", lines[2], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Unknown_program_gets_a_general_line(string? process)
    {
        foreach (var chapter in Enum.GetValues<TemptationChapter>())
        {
            Assert.Equal("Die Königin hält etwas Glänzendes in die Höhe.", Journey.TemptationLine(chapter, process));
        }
    }

    [Fact]
    public void Program_names_are_trimmed_and_long_ones_shortened()
    {
        Assert.EndsWith("Dich lockt discord.", Journey.TemptationLine(TemptationChapter.BeaverDam, "  discord "),
            StringComparison.Ordinal);

        var line = Journey.TemptationLine(TemptationChapter.Sledge, new string('x', 200));

        Assert.Contains(new string('x', 31) + "…", line, StringComparison.Ordinal);
        Assert.DoesNotContain(new string('x', 32), line, StringComparison.Ordinal);
    }

    [Fact]
    public void Undefined_chapter_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Journey.TemptationLine((TemptationChapter)42, "steam"));

    // ---- Wintermesser -------------------------------------------------------------------------------

    [Fact]
    public void Frost_allowances_follow_the_season_thresholds()
    {
        Assert.Equal(TimeSpan.FromSeconds(150), Journey.SpringFrostAllowance(TimeSpan.FromMinutes(25)));
        Assert.Equal(TimeSpan.FromSeconds(375), Journey.ThawFrostAllowance(TimeSpan.FromMinutes(25)));
        Assert.Equal(TimeSpan.Zero, Journey.SpringFrostAllowance(TimeSpan.Zero));
        Assert.Equal(TimeSpan.Zero, Journey.ThawFrostAllowance(TimeSpan.FromMinutes(-5)));
    }

    [Fact]
    public void Completed_watch_ends_in_the_season_the_allowances_promise()
    {
        var spring = Journey.SpringFrostAllowance(Plan);
        var thaw = Journey.ThawFrostAllowance(Plan);
        var oneSecond = TimeSpan.FromSeconds(1);

        Assert.Equal(RealmMood.Spring, RealmMoods.FromFrost((spring - oneSecond) / Plan));
        Assert.Equal(RealmMood.Thaw, RealmMoods.FromFrost(spring / Plan));
        Assert.Equal(RealmMood.Thaw, RealmMoods.FromFrost((thaw - oneSecond) / Plan));
        Assert.Equal(RealmMood.Winter, RealmMoods.FromFrost(thaw / Plan));
    }

    // ---- Abschlussbild ------------------------------------------------------------------------------

    [Theory]
    [InlineData(SessionPhase.Completed, RealmMood.Spring, JourneyEnding.Coronation)]
    [InlineData(SessionPhase.Completed, RealmMood.Thaw, JourneyEnding.Thaw)]
    [InlineData(SessionPhase.Completed, RealmMood.Winter, JourneyEnding.StoneCourtyard)]
    [InlineData(SessionPhase.Aborted, RealmMood.Spring, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Aborted, RealmMood.Thaw, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Aborted, RealmMood.Winter, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Running, RealmMood.Spring, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Running, RealmMood.Thaw, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Running, RealmMood.Winter, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Paused, RealmMood.Spring, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Paused, RealmMood.Thaw, JourneyEnding.Wardrobe)]
    [InlineData(SessionPhase.Paused, RealmMood.Winter, JourneyEnding.Wardrobe)]
    public void Ending_depends_on_outcome_and_season(SessionPhase outcome, RealmMood mood, JourneyEnding expected) =>
        Assert.Equal(expected, Journey.EndingFor(outcome, mood));

    [Fact]
    public void Every_ending_has_label_title_and_narration()
    {
        var texts = Enum.GetValues<JourneyEnding>().Select(Journey.Describe).ToList();

        Assert.All(texts, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Label));
            Assert.Equal(t.Label.ToUpperInvariant(), t.Label);
            Assert.False(string.IsNullOrWhiteSpace(t.Title));
            Assert.InRange(t.Narration.Length, 60, 260);
            Assert.DoesNotContain("{", t.Narration, StringComparison.Ordinal);
        });
        Assert.Equal(texts.Count, texts.Select(t => t.Title).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(new JourneyEndingText("FRÜHLING", "Krönung in Cair Paravel", texts[0].Narration), texts[0]);
        Assert.Equal("Zurück durch den Schrank", Journey.Describe(JourneyEnding.Wardrobe).Title);
    }

    [Fact]
    public void Wardrobe_narration_without_a_record_stays_general() =>
        Assert.StartsWith("Deine Reise endete vor Cair Paravel.", Journey.Describe(JourneyEnding.Wardrobe).Narration,
            StringComparison.Ordinal);

    [Fact]
    public void Completed_record_is_described_like_its_ending()
    {
        Assert.Equal(Journey.Describe(JourneyEnding.Coronation), Journey.Describe(Record(SessionPhase.Completed, 25, 1500, 30, 1)));
        Assert.Equal(Journey.Describe(JourneyEnding.StoneCourtyard), Journey.Describe(Record(SessionPhase.Completed, 25, 1500, 900, 12)));
    }

    [Fact]
    public void Undefined_ending_is_rejected() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Journey.Describe((JourneyEnding)42));

    [Fact]
    public void Record_endings_use_frost_over_measured_time()
    {
        Assert.Equal(JourneyEnding.Coronation, Journey.EndingFor(Record(SessionPhase.Completed, 25, 1500, 149, 2)));
        Assert.Equal(JourneyEnding.Thaw, Journey.EndingFor(Record(SessionPhase.Completed, 25, 1500, 150, 2)));
        Assert.Equal(JourneyEnding.StoneCourtyard, Journey.EndingFor(Record(SessionPhase.Completed, 25, 1500, 375, 9)));
        Assert.Equal(JourneyEnding.Wardrobe, Journey.EndingFor(Record(SessionPhase.Aborted, 25, 600, 0, 0)));
        Assert.Equal(RealmMood.Spring, Journey.MoodOf(Record(SessionPhase.Aborted, 25, 0, 0, 0)));
    }

    [Theory]
    [InlineData(560, "Deine Reise endete an Station 6: Das Tauwetter.")]    // 62 %
    [InlineData(350, "Deine Reise endete an Station 4: Der Biberdamm.")]    // 39 %
    [InlineData(0, "Deine Reise endete an Station 1: Der Laternenpfahl.")]
    public void Describing_an_aborted_record_names_its_station(double measuredSeconds, string expectedStart)
    {
        var text = Journey.Describe(Record(SessionPhase.Aborted, 15, measuredSeconds, 0, 2));

        Assert.Equal("ABGEBROCHEN", text.Label);
        Assert.Equal("Zurück durch den Schrank", text.Title);
        Assert.StartsWith(expectedStart, text.Narration, StringComparison.Ordinal);
        Assert.EndsWith("Die Laterne lässt sich jederzeit neu entzünden.", text.Narration, StringComparison.Ordinal);
    }

    // ---- Chronik-Titel -------------------------------------------------------------------------------

    [Fact]
    public void Flawless_completed_watch_keeps_the_box_closed() =>
        Assert.Equal("Die Schachtel blieb zu", Journey.ChronicleTitle(Record(SessionPhase.Completed, 25, 1500, 0, 0)));

    [Theory]
    [InlineData(560, 15, 0, "Zurück durch den Schrank · Station 6")]
    [InlineData(0, 25, 0, "Zurück durch den Schrank · Station 1")]
    [InlineData(1499, 25, 3, "Zurück durch den Schrank · Station 9")]
    public void Aborted_watch_goes_back_through_the_wardrobe(double measuredSeconds, int plannedMinutes,
        int distractions, string expected) =>
        Assert.Equal(expected, Journey.ChronicleTitle(
            Record(SessionPhase.Aborted, plannedMinutes, measuredSeconds, 0, distractions)));

    [Theory]
    [InlineData(100, "Krönung in Cair Paravel")]
    [InlineData(240, "Der Schlitten bleibt stecken")]
    [InlineData(450, "Im Hof der Steinfiguren")]
    public void Completed_watch_is_titled_by_its_season(double distractedSeconds, string expected) =>
        Assert.Equal(expected, Journey.ChronicleTitle(Record(SessionPhase.Completed, 25, 1500, distractedSeconds, 3)));

    [Fact]
    public void Chronicle_title_from_a_real_session()
    {
        var time = new ManualTimeProvider();
        var session = new FocusSession(TimeSpan.FromMinutes(9), time, ActivityState.Focused, "devenv");

        for (var i = 0; i < 300; i++)   // 5 Minuten → Station 6
        {
            time.Advance(TimeSpan.FromSeconds(1));
            session.Update(i is >= 100 and < 130 ? ActivityState.Distracted : ActivityState.Focused, "devenv");
        }

        session.Abort();
        var record = session.ToRecord();

        Assert.Equal(6, Journey.StationNumber(record.Measured, record.Planned));
        Assert.Equal("Zurück durch den Schrank · Station 6", Journey.ChronicleTitle(record));
    }
}
