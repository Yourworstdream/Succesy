using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Laternenwacht.App.Services.Native;

/// <summary>
/// Win32-Schnittstellen. Es werden ausschließlich lesende Abfragen und Fensterstile
/// des eigenen Fensters verwendet – keine Hooks, keine Tastaturprotokollierung.
/// </summary>
internal static partial class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const nint WS_EX_TOOLWINDOW = 0x00000080;
    internal const nint WS_EX_NOACTIVATE = 0x08000000;

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetLastInputInfo(ref LastInputInfo info);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtr(nint hWnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtr(nint hWnd, int index, nint newLong);

    [StructLayout(LayoutKind.Sequential)]
    internal struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }
}
