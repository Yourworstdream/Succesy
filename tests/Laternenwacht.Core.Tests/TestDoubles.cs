using Laternenwacht.Core.Abstractions;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tests;

/// <summary>Manuell vorstellbare Uhr für deterministische Zeittests.</summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _ticks;
    private DateTimeOffset _utcNow = new(2026, 9, 30, 8, 0, 0, TimeSpan.Zero);

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _ticks;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public void Advance(TimeSpan by)
    {
        _ticks += by.Ticks;
        _utcNow += by;
    }

    /// <summary>Verstellt nur die Wanduhr, nicht die monotone Zeit.</summary>
    public void TamperWallClock(TimeSpan by) => _utcNow += by;
}

internal sealed class ScriptedProbe : IActivityProbe
{
    public ActivitySnapshot Next { get; set; } = new("devenv", TimeSpan.Zero);

    public ActivitySnapshot Capture() => Next;
}

/// <summary>Ersatz für DPAPI: XOR mit fester Maske und Prüfbyte (nur für Tests!).</summary>
internal sealed class FakeProtector : ISecretProtector
{
    private const byte Mask = 0x5A;

    public bool FailOnUnprotect { get; set; }

    public byte[] Protect(byte[] plaintext) => [0x42, .. plaintext.Select(b => (byte)(b ^ Mask))];

    public byte[] Unprotect(byte[] protectedData)
    {
        if (FailOnUnprotect || protectedData.Length == 0 || protectedData[0] != 0x42)
        {
            throw new System.Security.Cryptography.CryptographicException("Schlüssel nicht entschlüsselbar.");
        }

        return protectedData.Skip(1).Select(b => (byte)(b ^ Mask)).ToArray();
    }
}

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "laternenwacht-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Aufräumen ist best effort.
        }
    }
}
