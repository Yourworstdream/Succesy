using System.Runtime.InteropServices;
using Laternenwacht.App.Services.Native;

namespace Laternenwacht.App.Views;

/// <summary>
/// Meldet die Fokusleiste bei Windows als Desktop-Symbolleiste („AppBar“, wie die Taskleiste) am oberen Rand an.
/// Windows hält den angemeldeten Streifen dann frei: Maximierte Fenster und der Arbeitsbereich beginnen unterhalb
/// der Leiste, statt von ihr überdeckt zu werden.
/// </summary>
/// <remarks>
/// <para>
/// Ablauf nach der Windows-Dokumentation: <c>ABM_NEW</c> (einmalig, mit eigener Rückrufnachricht aus
/// <c>RegisterWindowMessage</c>), dann <c>ABM_QUERYPOS</c> (Windows schiebt das Rechteck an anderen Leisten wie einer
/// oben angedockten Taskleiste vorbei) und <c>ABM_SETPOS</c>. <c>ABM_REMOVE</c> gibt den Platz wieder frei.
/// </para>
/// <para>
/// Keine Hooks: Die Shell schickt ihre Hinweise (<c>ABN_POSCHANGED</c>, <c>ABN_FULLSCREENAPP</c>) als Nachricht an das
/// eigene Fenster; das Fenster reicht sie über <see cref="TryHandleMessage"/> herein.
/// </para>
/// <para>
/// Neustart des Explorers (Absturz, Update, Task-Manager ▸ Neu starten): Die Shell verwirft dabei alle Anmeldungen.
/// Sie schickt danach allen Hauptfenstern die Rundmeldung <c>TaskbarCreated</c>; dann gilt die Leiste als nicht mehr
/// angemeldet, und <see cref="ShellRestarted"/> lässt das Fenster neu anmelden (ohne <c>ABM_REMOVE</c> – die alte
/// Anmeldung gibt es nicht mehr).
/// </para>
/// <para>
/// Aufräumen: Der Platz wird freigegeben, sobald die Leiste verborgen, frei verschoben oder die Option abgeschaltet
/// wird, beim Schließen des Fensters und – über <see cref="ReleaseActive"/> – beim Beenden der App sowie bei
/// unbehandelten Ausnahmen. Stürzt der Prozess dennoch hart ab, räumt die Shell verwaiste Leisten selbst auf, sobald sie
/// feststellt, dass das Fenster nicht mehr existiert.
/// </para>
/// <para>
/// Alle Rechtecke sind Gerätepixel (die App ist „Per Monitor V2“-DPI-bewusst). Die Höhe gibt der Aufrufer bereits in
/// Gerätepixeln an, damit die Reservierung zur DPI des Bildschirms passt.
/// </para>
/// </remarks>
internal sealed class AppBarDocking
{
    private const string CallbackMessageName = "Laternenwacht.Fokusleiste.AppBar";

    /// <summary>Rundmeldung der Shell nach ihrem (Neu-)Start – dieselbe, mit der sich Infobereichssymbole neu anmelden.</summary>
    private const string TaskbarCreatedMessageName = "TaskbarCreated";

    /// <summary>Die zuletzt angemeldete Leiste (es gibt nur eine) – für das Aufräumen beim Beenden oder Absturz.</summary>
    private static AppBarDocking? _active;

    private readonly nint _window;
    private readonly uint _callbackMessage;
    private readonly uint _taskbarCreatedMessage;
    private bool _registered;
    private nint _monitor;
    private int _height;
    private bool _stale;
    private NativeMethods.Rect _bounds;

    /// <param name="window">Fensterhandle der Fokusleiste (nach <c>SourceInitialized</c>).</param>
    public AppBarDocking(nint window)
    {
        if (window == 0)
        {
            throw new ArgumentException("Das Fenster hat noch kein Handle.", nameof(window));
        }

        _window = window;
        _callbackMessage = NativeMethods.RegisterWindowMessage(CallbackMessageName);
        _taskbarCreatedMessage = NativeMethods.RegisterWindowMessage(TaskbarCreatedMessageName);
    }

    /// <summary>Windows hat die Lage anderer Leisten geändert; der Streifen sollte neu ausgehandelt werden (<see cref="Reserve"/>).</summary>
    public event EventHandler? BoundsChanged;

    /// <summary>
    /// Der Explorer wurde neu gestartet und hat dabei alle Desktop-Symbolleisten vergessen. Die Leiste gilt jetzt als
    /// nicht angemeldet; das Fenster sollte neu reservieren (<see cref="Reserve"/>) und „stets oben“ erneut setzen.
    /// </summary>
    public event EventHandler? ShellRestarted;

    /// <summary>Ein Vollbildprogramm wurde geöffnet (<c>true</c>) oder geschlossen (<c>false</c>).</summary>
    public event EventHandler<bool>? FullScreenAppChanged;

    /// <summary>Ist die Leiste gerade als Desktop-Symbolleiste angemeldet?</summary>
    public bool IsRegistered => _registered;

    /// <summary>Der von Windows freigehaltene Streifen (Gerätepixel); nur gültig, solange <see cref="IsRegistered"/>.</summary>
    public NativeMethods.Rect Bounds => _bounds;

    /// <summary>Gibt den Platz der aktiven Leiste frei (beim Beenden der App oder nach einer unbehandelten Ausnahme).</summary>
    public static void ReleaseActive() => Volatile.Read(ref _active)?.Release();

    /// <summary>
    /// Reserviert am oberen Rand des Bildschirms <paramref name="monitor"/> einen Streifen der Höhe
    /// <paramref name="height"/> (Gerätepixel). Ist genau dieser Streifen schon reserviert, geschieht nichts: Jedes
    /// <c>ABM_SETPOS</c> ändert den Arbeitsbereich und löst bei Windows eine Rundmeldung aus. Deshalb wird auch nach
    /// einem Hinweis der Shell zuerst nur nachgefragt (<c>ABM_QUERYPOS</c>) und nur bei einem anderen Ergebnis neu gesetzt.
    /// </summary>
    /// <returns><c>true</c>, wenn der Streifen reserviert ist (dann gilt <see cref="Bounds"/>).</returns>
    public bool Reserve(nint monitor, int height)
    {
        if (monitor == 0 || height <= 0 || _callbackMessage == 0)
        {
            Release();
            return false;
        }

        var unchanged = _registered && monitor == _monitor && height == _height;
        if (unchanged && !_stale)
        {
            return true;
        }

        var info = new NativeMethods.MonitorInfo { Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (!NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            Release();
            return false;
        }

        var data = NewData();
        if (!_registered)
        {
            data.CallbackMessage = _callbackMessage;
            if (NativeMethods.SHAppBarMessage(NativeMethods.ABM_NEW, ref data) == 0)
            {
                return false;
            }

            _registered = true;
            Volatile.Write(ref _active, this);
        }

        // Wunsch: ganzer oberer Rand des Bildschirms. Windows rückt das Rechteck an anderen Leisten vorbei
        // (z. B. unter eine oben angedockte Taskleiste); danach gilt die gewünschte Höhe ab der zugeteilten Oberkante.
        data.Edge = NativeMethods.ABE_TOP;
        data.Bounds = new NativeMethods.Rect
        {
            Left = info.Monitor.Left,
            Top = info.Monitor.Top,
            Right = info.Monitor.Right,
            Bottom = info.Monitor.Top + height,
        };
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_QUERYPOS, ref data);
        data.Bounds.Bottom = data.Bounds.Top + height;
        _stale = false;

        if (unchanged && data.Bounds.Equals(_bounds))
        {
            return true;  // Shell-Hinweis ohne Auswirkung auf diese Leiste – nichts neu setzen (keine Rundmeldung)
        }

        NativeMethods.SHAppBarMessage(NativeMethods.ABM_SETPOS, ref data);
        _bounds = data.Bounds;
        _monitor = monitor;
        _height = height;
        return true;
    }

    /// <summary>Gibt den reservierten Streifen frei (<c>ABM_REMOVE</c>); ohne Anmeldung wirkungslos.</summary>
    public void Release()
    {
        if (!_registered)
        {
            return;
        }

        _registered = false;
        _monitor = 0;
        _height = 0;
        _stale = false;
        var data = NewData();
        NativeMethods.SHAppBarMessage(NativeMethods.ABM_REMOVE, ref data);
        Interlocked.CompareExchange(ref _active, null, this);
    }

    /// <summary>
    /// Wertet Fensternachrichten aus, die die Desktop-Symbolleiste betreffen. Wird aus der Fensterprozedur der
    /// Fokusleiste aufgerufen (eigenes Fenster, kein Hook).
    /// </summary>
    /// <returns><c>true</c>, wenn die Nachricht die eigene Rückrufnachricht war und damit erledigt ist.</returns>
    public bool TryHandleMessage(int message, nint wParam, nint lParam)
    {
        if (message == NativeMethods.WM_WINDOWPOSCHANGED)
        {
            // Empfehlung der Dokumentation: Die Shell über Lageänderungen der Leiste informieren.
            if (_registered)
            {
                var data = NewData();
                NativeMethods.SHAppBarMessage(NativeMethods.ABM_WINDOWPOSCHANGED, ref data);
            }

            return false;  // WPF braucht die Nachricht ebenfalls
        }

        if (_taskbarCreatedMessage != 0 && (uint)message == _taskbarCreatedMessage)
        {
            // Die alte Anmeldung existiert nicht mehr: nur den eigenen Zustand vergessen (kein ABM_REMOVE). Neu angemeldet
            // wird verzögert über das Fenster – nie verschachtelt innerhalb dieser Rundmeldung.
            _registered = false;
            _monitor = 0;
            _height = 0;
            _stale = false;
            _bounds = default;
            Interlocked.CompareExchange(ref _active, null, this);
            ShellRestarted?.Invoke(this, EventArgs.Empty);
            return false;  // Rundmeldung: auch andere Teile der App dürfen sie sehen
        }

        if (_callbackMessage == 0 || (uint)message != _callbackMessage)
        {
            return false;
        }

        switch ((int)wParam)
        {
            case NativeMethods.ABN_POSCHANGED when _registered:
                // Taskleiste oder eine andere Leiste hat sich bewegt: Beim nächsten Reserve neu nachfragen. Das Fenster
                // erledigt das verzögert über den Dispatcher – nie verschachtelt innerhalb dieser Shell-Nachricht.
                _stale = true;
                BoundsChanged?.Invoke(this, EventArgs.Empty);
                break;
            case NativeMethods.ABN_FULLSCREENAPP:
                FullScreenAppChanged?.Invoke(this, lParam != 0);
                break;
        }

        return true;
    }

    /// <summary>Lässt beim nächsten <see cref="Reserve"/> neu nachfragen (z. B. nach einem Wechsel der Bildschirme).</summary>
    public void Invalidate() => _stale = true;

    private NativeMethods.AppBarData NewData() => new()
    {
        Size = (uint)Marshal.SizeOf<NativeMethods.AppBarData>(),
        Window = _window,
    };
}
