using System.Security.Cryptography;
using Laternenwacht.Core.Abstractions;

namespace Laternenwacht.Core.Integrity;

/// <summary>
/// Verwaltet den geheimen HMAC-Schlüssel des Journals.
/// Der Schlüssel liegt nur geschützt (DPAPI, an das Windows-Benutzerkonto gebunden) auf dem Datenträger.
/// </summary>
public sealed class ProtectedKeyStore(string keyPath, ISecretProtector protector)
{
    public const int KeySizeBytes = 32;

    private readonly ISecretProtector _protector = protector ?? throw new ArgumentNullException(nameof(protector));

    public string KeyPath { get; } = keyPath ?? throw new ArgumentNullException(nameof(keyPath));

    /// <summary>Lädt den Schlüssel oder erzeugt einen neuen kryptografisch zufälligen Schlüssel.</summary>
    /// <exception cref="CryptographicException">Der vorhandene Schlüssel kann nicht entschlüsselt werden.</exception>
    public byte[] GetOrCreateKey()
    {
        if (File.Exists(KeyPath))
        {
            var key = _protector.Unprotect(File.ReadAllBytes(KeyPath));
            if (key.Length != KeySizeBytes)
            {
                throw new CryptographicException("Der gespeicherte Schlüssel hat eine unerwartete Länge.");
            }

            return key;
        }

        var fresh = RandomNumberGenerator.GetBytes(KeySizeBytes);
        AtomicFile.WriteAllBytes(KeyPath, _protector.Protect(fresh));
        return fresh;
    }
}
