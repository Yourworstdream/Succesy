using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Laternenwacht.Platform.Windows.Native;
using Laternenwacht.Core.Abstractions;
using Laternenwacht.Core.Model;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.Platform.Windows;

/// <summary>
/// Ermittelt den Prozess des Vordergrundfensters und die Leerlaufzeit.
/// Fenstertitel werden bewusst nicht gelesen (Datensparsamkeit); vom Programmpfad bleibt nur der Dateiname.
/// </summary>
/// <remarks>
/// Ressourcenschonend: Der Name wird nur neu ermittelt, wenn ein anderes Fenster in den Vordergrund kommt.
/// Ein Fenster gehört zeitlebens demselben Prozess – solange Fenster und Prozess-ID gleich bleiben, ist es
/// derselbe Prozess. Die Abfrage selbst öffnet den Prozess nur mit minimalem Leserecht und liest seinen Pfad,
/// statt ein <see cref="Process"/>-Objekt zu erzeugen (das je nach .NET-Version sogar eine Momentaufnahme
/// aller laufenden Prozesse anfordert).
/// Nicht threadsicher; wird ausschließlich vom Takt der Oberfläche aufgerufen.
/// </remarks>
public sealed class Win32ActivityProbe : IActivityProbe
{
    /// <summary>Puffer für den Programmpfad (Zeichen). Längere Pfade fallen auf den langsamen Weg zurück.</summary>
    private const int PathBufferLength = 1024;

    private nint _cachedWindow;
    private uint _cachedProcessId;
    private string? _cachedName;

    public ActivitySnapshot Capture() => new(GetForegroundProcessName(), GetIdleTime());

    private string? GetForegroundProcessName()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == 0 || NativeMethods.GetWindowThreadProcessId(hwnd, out var pid) == 0 || pid == 0)
        {
            return null;
        }

        if (hwnd == _cachedWindow && pid == _cachedProcessId)
        {
            return _cachedName;
        }

        var name = QueryImageName(pid) ?? QueryNameViaProcessSnapshot(pid);
        (_cachedWindow, _cachedProcessId, _cachedName) = (hwnd, pid, name);
        return name;
    }

    /// <summary>Schneller Weg: Programmpfad über ein Handle mit minimalem Zugriffsrecht.</summary>
    private static unsafe string? QueryImageName(uint pid)
    {
        using var process = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, inheritHandle: false, pid);
        if (process.IsInvalid)
        {
            return null;
        }

        var buffer = stackalloc char[PathBufferLength];
        var length = (uint)PathBufferLength;
        return NativeMethods.QueryFullProcessImageName(process, 0, buffer, ref length)
            ? ProcessNames.FromImagePath(new ReadOnlySpan<char>(buffer, (int)length))
            : null;
    }

    /// <summary>Rückfallweg für Sonderfälle (z. B. geschützte Systemprozesse oder überlange Pfade).</summary>
    private static string? QueryNameViaProcessSnapshot(uint pid)
    {
        try
        {
            using var process = Process.GetProcessById((int)pid);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            // Prozess bereits beendet oder Zugriff verweigert – als "unbekannt" behandeln.
            return null;
        }
    }

    private static TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LastInputInfo { Size = (uint)Marshal.SizeOf<NativeMethods.LastInputInfo>() };
        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        // Beide Werte sind 32-Bit-Millisekundenzähler; die vorzeichenlose Differenz ist überlaufsicher.
        var idleMs = unchecked((uint)Environment.TickCount - info.Time);
        return TimeSpan.FromMilliseconds(idleMs);
    }
}
