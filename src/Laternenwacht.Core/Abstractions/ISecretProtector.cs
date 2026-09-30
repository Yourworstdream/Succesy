namespace Laternenwacht.Core.Abstractions;

/// <summary>
/// Schützt Schlüsselmaterial im Ruhezustand (unter Windows per DPAPI, benutzergebunden).
/// </summary>
public interface ISecretProtector
{
    byte[] Protect(byte[] plaintext);

    byte[] Unprotect(byte[] protectedData);
}
