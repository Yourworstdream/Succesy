using System.Windows;
using System.Windows.Interop;
using Laternenwacht.App.Services.Native;

namespace Laternenwacht.App.Views;

internal static class WindowStyles
{
    /// <summary>
    /// Macht ein Fenster zum nicht aktivierenden Werkzeugfenster: kein Eintrag in Alt+Tab/Taskleiste,
    /// und ein Klick darauf stiehlt dem aktuellen Programm nicht den Fokus (sonst würde die Messung verfälscht).
    /// </summary>
    public static void MakeNonActivatingToolWindow(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE,
            style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
    }
}
