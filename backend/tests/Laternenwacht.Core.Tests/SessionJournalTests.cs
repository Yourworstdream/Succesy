using System.Text;
using Laternenwacht.Core.Integrity;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tests;

public sealed class SessionJournalTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly byte[] _key = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    public void Dispose() => _dir.Dispose();

    private SessionJournal NewJournal(byte[]? key = null) =>
        new(_dir.File("chronik.jsonl"), _dir.File("anker.json"), key ?? _key);

    private static SessionRecord Record(int distractedSeconds) => new()
    {
        Id = Guid.NewGuid(),
        StartedAtUtc = new DateTimeOffset(2026, 9, 30, 8, 0, 0, TimeSpan.Zero),
        EndedAtUtc = new DateTimeOffset(2026, 9, 30, 8, 25, 0, TimeSpan.Zero),
        Planned = TimeSpan.FromMinutes(25),
        Focused = TimeSpan.FromMinutes(25) - TimeSpan.FromSeconds(distractedSeconds),
        Distracted = TimeSpan.FromSeconds(distractedSeconds),
        Away = TimeSpan.Zero,
        DistractionCount = 1,
        Outcome = SessionPhase.Completed,
        TopDistractions = [new DistractionEntry("discord", TimeSpan.FromSeconds(distractedSeconds))],
    };

    private SessionJournal JournalWith(int entries)
    {
        var journal = NewJournal();
        journal.Verify();
        for (var i = 1; i <= entries; i++)
        {
            journal.Append(Record(i * 10));
        }

        return journal;
    }

    private string[] Lines() => File.ReadAllLines(_dir.File("chronik.jsonl"));

    private void WriteLines(IEnumerable<string> lines) =>
        File.WriteAllText(_dir.File("chronik.jsonl"), string.Join("\n", lines) + "\n", Encoding.UTF8);

    [Fact]
    public void New_journal_is_empty()
    {
        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Empty, result.Status);
        Assert.True(result.IsTrustworthy);
    }

    [Fact]
    public void Appended_entries_survive_a_reload_intact()
    {
        var original = JournalWith(3).Records;

        var reloaded = NewJournal();
        var result = reloaded.Verify();

        Assert.Equal(SealStatus.Intact, result.Status);
        Assert.Equal(3, result.ValidEntries);
        Assert.Equal(original.Select(r => (r.Id, r.Distracted, r.Outcome)), reloaded.Records.Select(r => (r.Id, r.Distracted, r.Outcome)));
        Assert.Equal(original[2].TopDistractions, reloaded.Records[2].TopDistractions);
    }

    [Fact]
    public void Modified_payload_breaks_the_seal()
    {
        JournalWith(3);
        var lines = Lines();
        lines[1] = lines[1].Replace("discord", "devenv", StringComparison.Ordinal);
        WriteLines(lines);

        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Broken, result.Status);
        Assert.Equal(2, result.BrokenAtSeq);
        Assert.Equal(1, result.ValidEntries);
    }

    [Fact]
    public void Deleted_middle_entry_is_detected()
    {
        JournalWith(3);
        WriteLines(Lines().Where((_, i) => i != 1));

        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Broken, result.Status);
        Assert.Equal(2, result.BrokenAtSeq);
    }

    [Fact]
    public void Swapped_entries_are_detected()
    {
        JournalWith(3);
        var lines = Lines();
        (lines[0], lines[1]) = (lines[1], lines[0]);
        WriteLines(lines);

        Assert.Equal(SealStatus.Broken, NewJournal().Verify().Status);
    }

    [Fact]
    public void Truncated_tail_is_detected_via_anchor()
    {
        JournalWith(3);
        WriteLines(Lines().Take(1));

        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Broken, result.Status);
        Assert.Contains("entfernt", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Deleted_journal_with_existing_anchor_is_detected()
    {
        JournalWith(2);
        File.Delete(_dir.File("chronik.jsonl"));

        Assert.Equal(SealStatus.Broken, NewJournal().Verify().Status);
    }

    [Fact]
    public void Forged_anchor_is_detected()
    {
        JournalWith(2);
        File.WriteAllText(_dir.File("anker.json"), "{\"seq\":1,\"mac\":\"00\",\"anchorMac\":\"00\"}");

        Assert.Equal(SealStatus.Broken, NewJournal().Verify().Status);
    }

    [Fact]
    public void Wrong_key_breaks_the_seal()
    {
        JournalWith(1);
        var otherKey = Enumerable.Repeat((byte)7, 32).ToArray();

        Assert.Equal(SealStatus.Broken, NewJournal(otherKey).Verify().Status);
    }

    [Fact]
    public void Anchor_lagging_by_one_entry_after_crash_is_repaired()
    {
        JournalWith(2);
        var anchorAfterTwo = File.ReadAllText(_dir.File("anker.json"));
        var journal = NewJournal();
        journal.Verify();
        journal.Append(Record(99));
        File.WriteAllText(_dir.File("anker.json"), anchorAfterTwo);  // Absturz simuliert: Anker nicht geschrieben

        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Intact, result.Status);
        Assert.Equal(3, result.ValidEntries);
    }

    [Fact]
    public void Garbage_line_is_reported_not_thrown()
    {
        JournalWith(1);
        File.AppendAllText(_dir.File("chronik.jsonl"), "{kein json}\n");

        var result = NewJournal().Verify();

        Assert.Equal(SealStatus.Broken, result.Status);
        Assert.Equal(2, result.BrokenAtSeq);
    }

    [Fact]
    public void Appending_to_unverified_journal_is_refused() =>
        Assert.Throws<InvalidOperationException>(() => NewJournal().Append(Record(1)));

    [Fact]
    public void Short_keys_are_rejected() =>
        Assert.Throws<ArgumentException>(() => NewJournal(new byte[16]));
}
