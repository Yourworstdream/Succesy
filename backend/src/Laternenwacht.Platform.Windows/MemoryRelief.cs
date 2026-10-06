using System.Runtime;
using Laternenwacht.Platform.Windows.Native;

namespace Laternenwacht.Platform.Windows;

/// <summary>
/// Gibt Arbeitsspeicher an Windows zurück, wenn die Anwendung in den Hintergrund tritt
/// (Hauptfenster minimiert oder versteckt): ein kompaktierender Aufräumlauf der Speicherbereinigung,
/// danach wird der Arbeitssatz geleert. Seiten, die noch gebraucht werden, lädt Windows bei Bedarf
/// aus dem Standby-Speicher nach – deshalb nur bei Zustandswechseln aufrufen, nie periodisch.
/// </summary>
public static class MemoryRelief
{
    public static void Release()
    {
        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, blocking: true, compacting: true);

        // (-1, -1) bedeutet laut Win32-Dokumentation: so viele Seiten wie möglich aus dem Arbeitssatz entfernen.
        _ = NativeMethods.SetProcessWorkingSetSize(NativeMethods.GetCurrentProcess(), -1, -1);
    }
}
