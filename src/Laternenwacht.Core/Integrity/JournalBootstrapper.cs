using System.Globalization;
using System.Security.Cryptography;
using Laternenwacht.Core.Abstractions;

namespace Laternenwacht.Core.Integrity;

/// <summary>Ergebnis beim Öffnen der Chronik.</summary>
/// <param name="Journal">Die beschreibbare Chronik.</param>
/// <param name="Verification">Prüfergebnis der vorgefundenen Chronik (vor einer eventuellen Archivierung).</param>
/// <param name="ArchivedTo">Ordner, in den eine gebrochene Chronik verschoben wurde, sonst <c>null</c>.</param>
public sealed record JournalOpenResult(SessionJournal Journal, JournalVerification Verification, string? ArchivedTo);

/// <summary>
/// Öffnet die Chronik im Datenverzeichnis und behandelt Fehlerfälle robust:
/// Eine gebrochene Chronik (oder ein nicht mehr entschlüsselbarer Schlüssel) wird
/// unverändert als Beweismittel archiviert, anschließend beginnt eine neue Kette.
/// </summary>
public static class JournalBootstrapper
{
    public const string JournalFileName = "chronik.jsonl";
    public const string AnchorFileName = "chronik.anker.json";
    public const string KeyFileName = "siegel.key";

    public static JournalOpenResult Open(string dataDirectory, ISecretProtector protector, TimeProvider time)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDirectory);
        ArgumentNullException.ThrowIfNull(protector);
        ArgumentNullException.ThrowIfNull(time);

        Directory.CreateDirectory(dataDirectory);
        var journalPath = Path.Combine(dataDirectory, JournalFileName);
        var anchorPath = Path.Combine(dataDirectory, AnchorFileName);
        var keyStore = new ProtectedKeyStore(Path.Combine(dataDirectory, KeyFileName), protector);

        byte[] key;
        try
        {
            key = keyStore.GetOrCreateKey();
        }
        catch (CryptographicException)
        {
            var verification = new JournalVerification(SealStatus.Broken, 0, null,
                "Der Siegelschlüssel gehört nicht zu diesem Benutzerkonto oder ist beschädigt.");
            var archive = Archive(dataDirectory, time, journalPath, anchorPath, keyStore.KeyPath);
            key = keyStore.GetOrCreateKey();
            return new JournalOpenResult(NewVerified(journalPath, anchorPath, key), verification, archive);
        }

        var journal = new SessionJournal(journalPath, anchorPath, key);
        var result = journal.Verify();
        if (result.IsTrustworthy)
        {
            return new JournalOpenResult(journal, result, null);
        }

        // Schlüssel bleibt erhalten: Die archivierte Chronik ist damit weiterhin nachprüfbar.
        var archivedTo = Archive(dataDirectory, time, journalPath, anchorPath);
        File.Copy(keyStore.KeyPath, Path.Combine(archivedTo, KeyFileName), overwrite: false);
        return new JournalOpenResult(NewVerified(journalPath, anchorPath, key), result, archivedTo);
    }

    private static SessionJournal NewVerified(string journalPath, string anchorPath, byte[] key)
    {
        var journal = new SessionJournal(journalPath, anchorPath, key);
        journal.Verify();
        return journal;
    }

    private static string Archive(string dataDirectory, TimeProvider time, params string[] files)
    {
        var stamp = time.GetUtcNow().ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var target = Path.Combine(dataDirectory, "archiv", "gebrochen-" + stamp);
        Directory.CreateDirectory(target);

        foreach (var file in files.Where(File.Exists))
        {
            File.Move(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        return target;
    }
}
