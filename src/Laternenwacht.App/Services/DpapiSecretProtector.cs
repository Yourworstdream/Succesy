using System.Security.Cryptography;
using System.Text;
using Laternenwacht.Core.Abstractions;

namespace Laternenwacht.App.Services;

/// <summary>
/// Schützt Geheimnisse mit der Windows Data Protection API.
/// Nur dasselbe Windows-Benutzerkonto kann die Daten wieder entschlüsseln.
/// </summary>
internal sealed class DpapiSecretProtector : ISecretProtector
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Laternenwacht.Siegel.v1");

    public byte[] Protect(byte[] plaintext) =>
        ProtectedData.Protect(plaintext, Entropy, DataProtectionScope.CurrentUser);

    public byte[] Unprotect(byte[] protectedData) =>
        ProtectedData.Unprotect(protectedData, Entropy, DataProtectionScope.CurrentUser);
}
