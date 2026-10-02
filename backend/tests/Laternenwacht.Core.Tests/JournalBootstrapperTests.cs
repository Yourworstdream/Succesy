using Laternenwacht.Core.Integrity;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tests;

public sealed class JournalBootstrapperTests : IDisposable
{
    private readonly TempDirectory _dir = new();
    private readonly FakeProtector _protector = new();
    private readonly ManualTimeProvider _time = new();

    public void Dispose() => _dir.Dispose();

    private static SessionRecord Record() => new()
    {
        Id = Guid.NewGuid(),
        StartedAtUtc = DateTimeOffset.UnixEpoch,
        EndedAtUtc = DateTimeOffset.UnixEpoch.AddMinutes(1),
        Planned = TimeSpan.FromMinutes(1),
        Focused = TimeSpan.FromMinutes(1),
        Distracted = TimeSpan.Zero,
        Away = TimeSpan.Zero,
        DistractionCount = 0,
        Outcome = SessionPhase.Completed,
    };

    [Fact]
    public void Creates_key_and_empty_journal_on_first_start()
    {
        var result = JournalBootstrapper.Open(_dir.Path, _protector, _time);

        Assert.Equal(SealStatus.Empty, result.Verification.Status);
        Assert.Null(result.ArchivedTo);
        Assert.True(File.Exists(_dir.File(JournalBootstrapper.KeyFileName)));
    }

    [Fact]
    public void Key_file_does_not_contain_the_plain_key()
    {
        JournalBootstrapper.Open(_dir.Path, _protector, _time);

        var stored = File.ReadAllBytes(_dir.File(JournalBootstrapper.KeyFileName));

        Assert.Equal(0x42, stored[0]);  // vom Schutzmechanismus gekennzeichnet
    }

    [Fact]
    public void Broken_journal_is_archived_and_a_fresh_chain_starts()
    {
        JournalBootstrapper.Open(_dir.Path, _protector, _time).Journal.Append(Record());
        File.AppendAllText(_dir.File(JournalBootstrapper.JournalFileName), "manipuliert\n");

        var result = JournalBootstrapper.Open(_dir.Path, _protector, _time);

        Assert.Equal(SealStatus.Broken, result.Verification.Status);
        Assert.NotNull(result.ArchivedTo);
        Assert.True(File.Exists(Path.Combine(result.ArchivedTo!, JournalBootstrapper.JournalFileName)));
        Assert.Empty(result.Journal.Records);
        result.Journal.Append(Record());  // neue Kette ist beschreibbar
    }

    [Fact]
    public void Undecryptable_key_leads_to_archive_instead_of_crash()
    {
        JournalBootstrapper.Open(_dir.Path, _protector, _time).Journal.Append(Record());
        File.WriteAllBytes(_dir.File(JournalBootstrapper.KeyFileName), [0x00, 0x01]);  // fremder/defekter Schlüssel

        var result = JournalBootstrapper.Open(_dir.Path, _protector, _time);

        Assert.Equal(SealStatus.Broken, result.Verification.Status);
        Assert.NotNull(result.ArchivedTo);
        Assert.Empty(result.Journal.Records);
    }
}
