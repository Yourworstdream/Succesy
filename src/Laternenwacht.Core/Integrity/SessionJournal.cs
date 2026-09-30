using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Integrity;

/// <summary>
/// Manipulationserkennendes Sitzungsjournal ("Chronik").
/// </summary>
/// <remarks>
/// Jede Zeile wird mit HMAC-SHA256 versiegelt und enthält das Siegel ihres Vorgängers
/// (Hash-Kette). Dadurch fallen Änderungen, Einfügungen, Löschungen und Vertauschungen auf.
/// Ein separat versiegelter Anker auf den letzten Eintrag macht zusätzlich das
/// Abschneiden am Ende sichtbar.
/// </remarks>
public sealed class SessionJournal
{
    public const int MaxLineLength = 16 * 1024;
    public const long MaxFileSize = 20L * 1024 * 1024;

    internal static readonly string GenesisSeal = new('0', 64);

    private readonly byte[] _key;
    private readonly object _sync = new();
    private readonly List<SessionRecord> _records = [];
    private long _lastSeq;
    private string _lastMac = GenesisSeal;
    private JournalVerification? _verification;

    public SessionJournal(string journalPath, string anchorPath, byte[] key)
    {
        ArgumentException.ThrowIfNullOrEmpty(journalPath);
        ArgumentException.ThrowIfNullOrEmpty(anchorPath);
        ArgumentNullException.ThrowIfNull(key);
        if (key.Length < 32)
        {
            throw new ArgumentException("Der Schlüssel muss mindestens 256 Bit lang sein.", nameof(key));
        }

        JournalPath = journalPath;
        AnchorPath = anchorPath;
        _key = (byte[])key.Clone();
    }

    public string JournalPath { get; }

    public string AnchorPath { get; }

    /// <summary>Alle Einträge, deren Siegel geprüft und für gültig befunden wurden.</summary>
    public IReadOnlyList<SessionRecord> Records
    {
        get
        {
            lock (_sync)
            {
                return _records.ToList();
            }
        }
    }

    /// <summary>Prüft sämtliche Siegel und lädt die gültigen Einträge.</summary>
    public JournalVerification Verify()
    {
        lock (_sync)
        {
            _records.Clear();
            _lastSeq = 0;
            _lastMac = GenesisSeal;
            _verification = VerifyCore();
            return _verification;
        }
    }

    /// <summary>Hängt ein versiegeltes Sitzungsergebnis an.</summary>
    /// <exception cref="InvalidOperationException">Das Journal wurde nicht geprüft oder ist gebrochen.</exception>
    public void Append(SessionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        lock (_sync)
        {
            if (_verification is not { IsTrustworthy: true })
            {
                throw new InvalidOperationException("In eine ungeprüfte oder gebrochene Chronik wird nicht geschrieben.");
            }

            var seq = _lastSeq + 1;
            var payload = JsonSerializer.Serialize(record, CoreJsonContext.Default.SessionRecord);
            var entry = new JournalEntry(seq, _lastMac, payload, ComputeEntryMac(seq, _lastMac, payload));
            var line = JsonSerializer.Serialize(entry, CoreJsonContext.Default.JournalEntry);

            if (line.Length > MaxLineLength)
            {
                throw new InvalidOperationException("Der Eintrag ist zu groß für die Chronik.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(JournalPath))!);
            using (var stream = new FileStream(JournalPath, FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                stream.Write(Encoding.UTF8.GetBytes(line + "\n"));
                stream.Flush(flushToDisk: true);
            }

            WriteAnchor(seq, entry.Mac);

            _records.Add(record);
            _lastSeq = seq;
            _lastMac = entry.Mac;
            _verification = new JournalVerification(SealStatus.Intact, _records.Count, null, IntactMessage(_records.Count));
        }
    }

    private JournalVerification VerifyCore()
    {
        var anchor = ReadAnchor(out var anchorError);
        if (anchorError is not null)
        {
            return Broken(null, anchorError);
        }

        if (!File.Exists(JournalPath))
        {
            return anchor is null
                ? new JournalVerification(SealStatus.Empty, 0, null, "Die Chronik ist noch unbeschrieben.")
                : Broken(1, "Die Chronik fehlt, obwohl ein Anker existiert – sie wurde gelöscht.");
        }

        if (new FileInfo(JournalPath).Length > MaxFileSize)
        {
            return Broken(null, "Die Chronik ist unplausibel groß.");
        }

        var expectedPrev = GenesisSeal;
        long expectedSeq = 1;
        var macsBySeq = new Dictionary<long, string>();

        foreach (var line in File.ReadLines(JournalPath, Encoding.UTF8))
        {
            if (line.Length == 0)
            {
                continue;
            }

            if (line.Length > MaxLineLength)
            {
                return Broken(expectedSeq, $"Eintrag {expectedSeq} ist unzulässig lang.");
            }

            JournalEntry? entry;
            SessionRecord? record;
            try
            {
                entry = JsonSerializer.Deserialize(line, CoreJsonContext.Default.JournalEntry);
                record = entry is null ? null : JsonSerializer.Deserialize(entry.Payload, CoreJsonContext.Default.SessionRecord);
            }
            catch (JsonException)
            {
                return Broken(expectedSeq, $"Eintrag {expectedSeq} ist unlesbar.");
            }

            if (entry is null || record is null)
            {
                return Broken(expectedSeq, $"Eintrag {expectedSeq} ist leer.");
            }

            if (entry.Seq != expectedSeq)
            {
                return Broken(expectedSeq, $"Erwartet war Eintrag {expectedSeq}, gefunden wurde {entry.Seq} – Einträge fehlen oder wurden vertauscht.");
            }

            if (!FixedTimeEqualsHex(entry.Prev, expectedPrev))
            {
                return Broken(expectedSeq, $"Die Kette reißt vor Eintrag {expectedSeq}.");
            }

            if (!FixedTimeEqualsHex(entry.Mac, ComputeEntryMac(entry.Seq, entry.Prev, entry.Payload)))
            {
                return Broken(expectedSeq, $"Das Siegel von Eintrag {expectedSeq} ist gebrochen – der Inhalt wurde verändert.");
            }

            _records.Add(record);
            macsBySeq[entry.Seq] = entry.Mac;
            _lastSeq = entry.Seq;
            _lastMac = entry.Mac;
            expectedPrev = entry.Mac;
            expectedSeq++;
        }

        if (_lastSeq == 0)
        {
            return anchor is null
                ? new JournalVerification(SealStatus.Empty, 0, null, "Die Chronik ist noch unbeschrieben.")
                : Broken(1, "Die Chronik wurde geleert, obwohl ein Anker existiert.");
        }

        if (anchor is null)
        {
            return Broken(null, "Der Anker der Chronik fehlt.");
        }

        if (anchor.Seq == _lastSeq && FixedTimeEqualsHex(anchor.Mac, _lastMac))
        {
            return new JournalVerification(SealStatus.Intact, _records.Count, null, IntactMessage(_records.Count));
        }

        // Absturz zwischen Journalzeile und Anker: Der Anker hinkt genau einen Eintrag hinterher.
        // Der neue Eintrag ist selbst gültig versiegelt, daher wird der Anker nachgezogen.
        if (anchor.Seq == _lastSeq - 1 && macsBySeq.TryGetValue(anchor.Seq, out var previousMac) && FixedTimeEqualsHex(anchor.Mac, previousMac))
        {
            WriteAnchor(_lastSeq, _lastMac);
            return new JournalVerification(SealStatus.Intact, _records.Count, null, IntactMessage(_records.Count));
        }

        return Broken(anchor.Seq, $"Der Anker verweist auf Eintrag {anchor.Seq}, die Chronik endet bei {_lastSeq} – Einträge wurden entfernt.");
    }

    private JournalAnchor? ReadAnchor(out string? error)
    {
        error = null;
        if (!File.Exists(AnchorPath))
        {
            return null;
        }

        try
        {
            var info = new FileInfo(AnchorPath);
            if (info.Length > 4096)
            {
                error = "Der Anker der Chronik ist beschädigt.";
                return null;
            }

            var anchor = JsonSerializer.Deserialize(File.ReadAllText(AnchorPath), CoreJsonContext.Default.JournalAnchor);
            if (anchor is null || !FixedTimeEqualsHex(anchor.AnchorMac, ComputeAnchorMac(anchor.Seq, anchor.Mac)))
            {
                error = "Das Siegel des Ankers ist gebrochen.";
                return null;
            }

            return anchor;
        }
        catch (JsonException)
        {
            error = "Der Anker der Chronik ist unlesbar.";
            return null;
        }
    }

    private void WriteAnchor(long seq, string mac)
    {
        var anchor = new JournalAnchor(seq, mac, ComputeAnchorMac(seq, mac));
        AtomicFile.WriteAllText(AnchorPath, JsonSerializer.Serialize(anchor, CoreJsonContext.Default.JournalAnchor));
    }

    private string ComputeEntryMac(long seq, string prev, string payload) =>
        Mac($"entry\n{seq.ToString(CultureInfo.InvariantCulture)}\n{prev}\n{payload}");

    private string ComputeAnchorMac(long seq, string mac) =>
        Mac($"anchor\n{seq.ToString(CultureInfo.InvariantCulture)}\n{mac}");

    private string Mac(string message) =>
        Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(message))).ToLowerInvariant();

    private static bool FixedTimeEqualsHex(string? a, string? b) =>
        a is not null && b is not null && a.Length == b.Length &&
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    private JournalVerification Broken(long? seq, string message) =>
        new(SealStatus.Broken, _records.Count, seq, message);

    private static string IntactMessage(int count) =>
        count == 1 ? "Das Siegel ist unversehrt – 1 Wacht verzeichnet." : $"Alle Siegel sind unversehrt – {count} Wachten verzeichnet.";
}
