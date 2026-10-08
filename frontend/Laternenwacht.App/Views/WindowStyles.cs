using System.Windows;
using System.Windows.Interop;
using Laternenwacht.App.Services.Native;

namespace Laternenwacht.App.Views;

internal static class WindowStyles
{
    private const uint KeepPlaceFlags =
        NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER;

    /// <summary>
    /// Macht ein Fenster zum nicht aktivierenden Werkzeugfenster: kein Eintrag in Alt+Tab/Taskleiste,
    /// und ein Klick darauf stiehlt dem aktuellen Programm nicht den Fokus (sonst würde die Messung verfälscht).
    /// </summary>
    /// <remarks>
    /// Windows übernimmt geänderte erweiterte Stile erst nach <c>SetWindowPos</c> mit <c>SWP_FRAMECHANGED</c>
    /// vollständig; ohne diesen Aufruf bleiben Teile des alten Zustands zwischengespeichert.
    /// </remarks>
    public static void MakeNonActivatingToolWindow(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE,
            style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
        NativeMethods.SetWindowPos(handle, 0, 0, 0, 0, 0,
            KeepPlaceFlags | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED);
    }

    /// <summary>
    /// Setzt das Fenster an die Spitze der „stets oben“-Fenster (<c>HWND_TOPMOST</c>) bzw. zurück zu den gewöhnlichen
    /// Fenstern (<c>HWND_NOTOPMOST</c>) – ohne es zu verschieben, zu vergrößern oder zu aktivieren.
    /// </summary>
    public static void SetTopmost(nint handle, bool topmost)
    {
        if (handle != 0)
        {
            NativeMethods.SetWindowPos(handle, topmost ? NativeMethods.HWND_TOPMOST : NativeMethods.HWND_NOTOPMOST,
                0, 0, 0, 0, KeepPlaceFlags);
        }
    }

    /// <summary>Trägt das Fenster gerade das Merkmal „stets oben“ (<c>WS_EX_TOPMOST</c>)?</summary>
    public static bool IsTopmost(nint handle) =>
        handle != 0 && (NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE) & NativeMethods.WS_EX_TOPMOST) != 0;
}
