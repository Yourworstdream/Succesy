using System.Runtime.InteropServices;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Laternenwacht.App.Services.Native;

/// <summary>Win32-Fensterstile für die eigenen, randlosen Fenster (Fokusleiste, Rabenbote).</summary>
internal static partial class NativeMethods
{
    internal const int GWL_EXSTYLE = -20;
    internal const nint WS_EX_TOOLWINDOW = 0x00000080;
    internal const nint WS_EX_NOACTIVATE = 0x08000000;

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    internal static partial nint GetWindowLongPtr(nint hWnd, int index);

    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    internal static partial nint SetWindowLongPtr(nint hWnd, int index, nint newLong);
}
