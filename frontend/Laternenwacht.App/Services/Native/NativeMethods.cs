using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Laternenwacht.App.Services.Native;

/// <summary>
/// Win32-Schnittstellen für die eigenen, randlosen Fenster (Fokusleiste, Rabenbote): Fensterstile, Z-Reihenfolge,
/// Bildschirmmaße und die Anmeldung als Desktop-Symbolleiste. Keine Hooks – weder global noch für fremde Fenster;
/// abgefragt wird nur, <em>welches</em> Fenster im Vordergrund ist (nie Titel oder Inhalt).
/// </summary>
internal static partial class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const nint WS_EX_TOPMOST = 0x00000008;
    internal const nint WS_EX_TOOLWINDOW = 0x00000080;
    internal const nint WS_EX_NOACTIVATE = 0x08000000;

    // SetWindowPos: Einfügeposition in der Z-Reihenfolge
    internal const nint HWND_TOPMOST = -1;
    internal const nint HWND_NOTOPMOST = -2;

    // SetWindowPos: Schalter
    internal const uint SWP_NOSIZE = 0x0001;
    internal const uint SWP_NOMOVE = 0x0002;
    internal const uint SWP_NOZORDER = 0x0004;
    internal const uint SWP_NOACTIVATE = 0x0010;
    internal const uint SWP_FRAMECHANGED = 0x0020;
    internal const uint SWP_NOOWNERZORDER = 0x0200;

    // Fensternachrichten
    internal const int WM_WINDOWPOSCHANGED = 0x0047;
    internal const int WM_DISPLAYCHANGE = 0x007E;
    internal const int WM_SETTINGCHANGE = 0x001A;

    // MonitorFromPoint / MonitorFromWindow
    internal const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    internal const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;

    // SHAppBarMessage: Nachrichten an die Shell
    internal const uint ABM_NEW = 0x00000000;
    internal const uint ABM_REMOVE = 0x00000001;
    internal const uint ABM_QUERYPOS = 0x00000002;
    internal const uint ABM_SETPOS = 0x00000003;
    internal const uint ABM_WINDOWPOSCHANGED = 0x00000009;

    // SHAppBarMessage: Benachrichtigungen der Shell (wParam der eigenen Rückrufnachricht)
    internal const int ABN_STATECHANGE = 0x0000000;
    internal const int ABN_POSCHANGED = 0x0000001;
    internal const int ABN_FULLSCREENAPP = 0x0000002;

    /// <summary>Kante der Desktop-Symbolleiste: oben.</summary>
    internal const uint ABE_TOP = 1;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtr(nint hWnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtr(nint hWnd, int index, nint newLong);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hWnd, out Rect rect);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromPoint(Point point, uint flags);

    [LibraryImport("user32.dll")]
    internal static partial nint MonitorFromWindow(nint hWnd, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    /// <summary>Wirksame DPI eines Bildschirms (<c>MDT_EFFECTIVE_DPI</c> = 0); Rückgabe 0 = Erfolg (HRESULT).</summary>
    [LibraryImport("shcore.dll")]
    internal static partial int GetDpiForMonitor(nint monitor, int dpiType, out uint dpiX, out uint dpiY);

    [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial uint RegisterWindowMessage(string name);

    [LibraryImport("shell32.dll")]
    internal static partial nuint SHAppBarMessage(uint message, ref AppBarData data);

    /// <summary>Win32-RECT (Gerätepixel).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly int Width => Right - Left;

        public readonly int Height => Bottom - Top;
    }

    /// <summary>Win32-POINT (Gerätepixel).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Point
    {
        public int X;
        public int Y;
    }

    /// <summary>MONITORINFO: ganzer Bildschirm und Arbeitsbereich (ohne Taskleiste und Desktop-Symbolleisten).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    /// <summary>
    /// WINDOWPOS aus <c>lParam</c> von <c>WM_WINDOWPOSCHANGED</c> (nur gelesen, über <see cref="Marshal.OffsetOf{T}(string)"/>
    /// und <see cref="Marshal.ReadInt32(nint, int)"/> – ohne Kopie und ohne Speicheranforderung).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WindowPos
    {
        public nint Window;
        public nint InsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public uint Flags;
    }

    /// <summary>APPBARDATA für <see cref="SHAppBarMessage"/>.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct AppBarData
    {
        public uint Size;
        public nint Window;
        public uint CallbackMessage;
        public uint Edge;
        public Rect Bounds;
        public nint LParam;
    }
}
