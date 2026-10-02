using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Laternenwacht.Platform.Windows.Native;
using Laternenwacht.Core.Abstractions;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Platform.Windows;

/// <summary>
/// Ermittelt den Prozess des Vordergrundfensters und die Leerlaufzeit.
/// Fenstertitel werden bewusst nicht gelesen (Datensparsamkeit).
/// </summary>
public sealed class Win32ActivityProbe : IActivityProbe
{
    public ActivitySnapshot Capture() => new(GetForegroundProcessName(), GetIdleTime());

    private static string? GetForegroundProcessName()
    {
        var hwnd = NativeMethods.GetForegroundWindow();
        if (hwnd == 0 || NativeMethods.GetWindowThreadProcessId(hwnd, out var pid) == 0 || pid == 0)
        {
            return null;
        }

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
