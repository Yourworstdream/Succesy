using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using Laternenwacht.App.Services.Native;
using Laternenwacht.Core.Settings;

namespace Laternenwacht.App.Views;

/// <summary>
/// Randlose, stets sichtbare Fokusleiste in vier Größen (<see cref="BarSize"/>). Standardmäßig sitzt sie mittig am oberen
/// Bildschirmrand; per Maus lässt sie sich frei verschieben, die Position wird gespeichert.
/// Sie stiehlt beim Anklicken nicht den Fokus – sonst würde sie selbst die Messung verfälschen.
/// </summary>
/// <remarks>
/// <para>
/// <b>Warum die Leiste früher unter Arbeitsfenster geraten konnte:</b> WPF setzt „stets oben“ (<c>HWND_TOPMOST</c>) nur
/// beim Erzeugen des Fensters und wenn sich <see cref="Window.Topmost"/> ändert. Innerhalb der Gruppe der „stets oben“-
/// Fenster liegt aber immer das zuletzt aktivierte oder gezeigte vorn – und die Leiste wird nie aktiviert
/// (<c>WS_EX_NOACTIVATE</c>), kommt also von selbst nie wieder nach vorn. Außerdem kann Windows sie beim Verbergen und
/// Wiederzeigen zu Beginn jeder Wacht, bei „Desktop anzeigen“, Vollbildwechseln oder Änderungen der Bildschirme hinter
/// andere Fenster schieben, ohne dass WPF das bemerkt. Und angedockt teilte sie sich den oberen Streifen mit den
/// Titelleisten maximierter Fenster.
/// </para>
/// <para>
/// <b>Abhilfe ohne Hooks:</b> Nach jedem Zeigen, beim Erzeugen des Fensters und nach Bildschirmänderungen wird
/// <c>HWND_TOPMOST</c> per <c>SetWindowPos</c> erneut gesetzt. Solange die Leiste sichtbar ist, prüft ein ruhiger
/// Sekundentakt (<see cref="ZOrderCheckInterval"/>) mit zwei billigen Abfragen, ob ein anderes Fenster in den Vordergrund
/// gekommen ist oder das Merkmal „stets oben“ verloren ging, und setzt es nur dann erneut. Fenster, die selbst „stets oben“
/// sind (Startmenü, Alt+Tab, Taskansicht, Kontextmenüs), dürfen vorn bleiben, solange sie im Vordergrund sind.
/// Optional reserviert <see cref="AppBarDocking"/> den Streifen am oberen Rand, damit maximierte Fenster darunter beginnen.
/// Bei <see cref="FocusSettings.BarAlwaysOnTop"/> = <c>false</c> gilt <c>HWND_NOTOPMOST</c>, und nichts wird erneut gesetzt.
/// </para>
/// <para>
/// <b>Lage erst nach dem Layout:</b> Das Fenster folgt seiner Vorlage (<c>SizeToContent</c>); die Größe des HWND ändert
/// WPF aber erst am Ende des Layout-Durchgangs. <see cref="Reposition"/> stellt die Leiste deshalb nicht sofort, sondern
/// gebündelt mit der Priorität <see cref="DispatcherPriority.Loaded"/> – also nach dem Layout – an ihren Platz, und jede
/// echte Größenänderung des HWND (<c>WM_WINDOWPOSCHANGED</c> ohne <c>SWP_NOSIZE</c>, auch durch einen DPI-Wechsel) stößt
/// das erneut an. So rechnen Andocken und Eingrenzen immer mit der tatsächlichen Breite in Gerätepixeln.
/// </para>
/// </remarks>
public partial class FocusBarWindow : Window
{
    /// <summary>Breite der ultradünnen Leiste, wenn sie frei verschoben (nicht angedockt) ist.</summary>
    public const double ThinFloatingWidth = 640;

    public static readonly DependencyProperty ThinStripWidthProperty = DependencyProperty.Register(
        nameof(ThinStripWidth), typeof(double), typeof(FocusBarWindow), new PropertyMetadata(ThinFloatingWidth));

    public static readonly DependencyProperty IsReservingTopProperty = DependencyProperty.Register(
        nameof(IsReservingTop), typeof(bool), typeof(FocusBarWindow), new PropertyMetadata(false));

    /// <summary>Lage der Felder in WINDOWPOS (einmal bestimmt; passt für 32 und 64 Bit).</summary>
    private static readonly int WindowPosWidthOffset = (int)Marshal.OffsetOf<NativeMethods.WindowPos>(nameof(NativeMethods.WindowPos.Width));
    private static readonly int WindowPosHeightOffset = (int)Marshal.OffsetOf<NativeMethods.WindowPos>(nameof(NativeMethods.WindowPos.Height));
    private static readonly int WindowPosFlagsOffset = (int)Marshal.OffsetOf<NativeMethods.WindowPos>(nameof(NativeMethods.WindowPos.Flags));

    /// <summary>Takt der Vordergrund-Prüfung – nur solange die Leiste sichtbar und „stets oben“ ist.</summary>
    private static readonly TimeSpan ZOrderCheckInterval = TimeSpan.FromSeconds(1);

    private readonly DispatcherTimer _zOrderWatch;
    private AppBarDocking? _appBar;
    private HwndSource? _source;
    private nint _handle;
    private nint _lastForeground;
    private double? _customLeft;
    private double? _customTop;

    /// <summary>
    /// Die frei gewählte Position, die zuletzt an das Fenster übergeben wurde. Solange sie gilt, wird nur noch
    /// eingegrenzt – sonst spränge eine eingegrenzte Leiste bei jeder Schnelleinstellung kurz an die alte Stelle.
    /// </summary>
    private (double Left, double Top)? _appliedCustom;
    private bool _dragging;
    private bool _repositionQueued;
    private bool _closed;
    private (int Width, int Height) _pixelSize;
    private bool _alwaysOnTop = true;
    private bool _reservesSpace = true;
    private BarSize _size = BarSize.Large;

    public FocusBarWindow()
    {
        InitializeComponent();
        _zOrderWatch = new DispatcherTimer(DispatcherPriority.Background) { Interval = ZOrderCheckInterval };
        _zOrderWatch.Tick += (_, _) => CheckZOrder();
        SourceInitialized += OnSourceInitialized;
        // Größenwechsel: Erst wenn das HWND die neue Größe hat (WM_WINDOWPOSCHANGED, siehe WndProc), wird neu gestellt.
        // Bis zum ersten Handle genügt die WPF-Größe.
        SizeChanged += (_, _) =>
        {
            if (_handle == 0)
            {
                Reposition();
            }
        };
        Loaded += (_, _) => Reposition();
        IsVisibleChanged += OnIsVisibleChanged;
        DpiChanged += (_, _) => RefreshLayout();
        MouseLeftButtonDown += OnDragStart;
        Closed += OnClosed;
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
    }

    /// <summary>Der Benutzer hat die Leiste an eine neue Stelle gezogen (geräteunabhängige Pixel).</summary>
    public event EventHandler<Point>? PositionChosen;

    /// <summary>
    /// Angedockt mit freigehaltenem Streifen: Die Vorlagen ziehen dann den unteren Rand auf den Saum ein und werfen keinen
    /// nach unten fallenden Schatten. So ist das Fenster genau so hoch wie der reservierte Streifen
    /// (<see cref="ReservedHeight"/>) – kein Schattenpixel ragt über die Titel- oder Tab-Leiste maximierter Fenster und
    /// fängt dort Klicks ab (ein geschichtetes Fenster nimmt jeden nicht völlig transparenten Pixel als Treffer).
    /// </summary>
    public bool IsReservingTop
    {
        get => (bool)GetValue(IsReservingTopProperty);
        private set => SetValue(IsReservingTopProperty, value);
    }

    /// <summary>
    /// Breite der ultradünnen Leiste: angedockt mit freigehaltenem Streifen die Breite des Arbeitsbereichs, sonst
    /// <see cref="ThinFloatingWidth"/> (ohne Reservierung läge sonst ein klickfangendes Band über der ganzen Oberkante).
    /// </summary>
    public double ThinStripWidth
    {
        get => (double)GetValue(ThinStripWidthProperty);
        set => SetValue(ThinStripWidthProperty, value);
    }

    /// <summary>
    /// Übernimmt Größe, „stets oben“ und „Platz freihalten“ aus den Einstellungen. Die Vorlage der Größe wählt das
    /// Fenster selbst (Datentrigger auf <c>Settings.BarSize</c>); hier geht es um Lage, Z-Reihenfolge und Reservierung.
    /// </summary>
    public void ApplyBarSettings(BarSize size, bool alwaysOnTop, bool reservesSpace)
    {
        _size = Enum.IsDefined(size) ? size : BarSize.Large;
        _reservesSpace = reservesSpace;
        if (_alwaysOnTop != alwaysOnTop || Topmost != alwaysOnTop)
        {
            _alwaysOnTop = alwaysOnTop;
            Topmost = alwaysOnTop;  // WPF-Eigenschaft synchron halten
            WindowStyles.SetTopmost(_handle, alwaysOnTop);
        }

        UpdateZOrderWatch();
        Reposition();
    }

    /// <summary>Setzt eine frei gewählte Position oder – mit <c>null</c> – das Andocken am oberen Rand.</summary>
    public void ApplyPosition(double? left, double? top)
    {
        (_customLeft, _customTop) = left is not null && top is not null ? (left, top) : (null, null);
        Reposition();
    }

    /// <summary>Zeigt die Leiste (zu Beginn einer Wacht), stellt sie an ihren Platz und holt sie nach vorn.</summary>
    public void ShowOnTop()
    {
        Show();
        Reposition();
        AssertZOrder();
    }

    /// <summary>
    /// Stellt die Leiste an ihre gespeicherte Position bzw. dockt sie oben an (und reserviert dort ggf. Platz) – gebündelt
    /// und erst nach dem anstehenden Layout (<see cref="DispatcherPriority.Loaded"/>), damit eine gerade gewechselte
    /// Größe schon im HWND angekommen ist. Mehrere Aufrufe hintereinander führen zu einer einzigen Neuberechnung.
    /// </summary>
    public void Reposition()
    {
        if (_repositionQueued || _closed)
        {
            return;
        }

        _repositionQueued = true;
        Dispatcher.InvokeAsync(RepositionNow, DispatcherPriority.Loaded);
    }

    private void RepositionNow()
    {
        _repositionQueued = false;
        if (_dragging || _closed)
        {
            return;
        }

        if (_customLeft is { } left && _customTop is { } top)
        {
            // Frei verschoben: kein reservierter Streifen, ultradünn in fester Breite, ganz auf dem Bildschirm.
            _appBar?.Release();
            var reshaped = SetReservingTop(false) | SetThinWidth(ThinFloatingWidth);
            if (reshaped && _handle != 0)
            {
                // Erst die neue Form ins HWND bringen lassen, dann mit der echten Größe stellen und eingrenzen.
                Reposition();
                return;
            }

            if (_appliedCustom is not { } applied || applied.Left != left || applied.Top != top)
            {
                Left = left;
                Top = top;
                _appliedCustom = (left, top);
            }

            ClampOntoScreen();
            return;
        }

        _appliedCustom = null;
        DockAtTop();
    }

    /// <summary>
    /// Reservierte Höhe je Größe (geräteunabhängige Pixel) = Höhe des ganzen Fensters bei <see cref="IsReservingTop"/>.
    /// </summary>
    /// <remarks>
    /// Muss zu den Vorlagen in FocusBarWindow.xaml passen (Stile *Frame mit Datentrigger auf IsReservingTop):
    /// Rand oben + Kapsel samt Saum oben und unten + eingezogener Rand unten. Ist das Fenster höher als der Streifen,
    /// fängt der Überhang Klicks ab, die für maximierte Fenster gedacht sind.
    /// </remarks>
    internal static double ReservedHeight(BarSize size) => size switch
    {
        BarSize.UltraThin => 8,   // Streifen 6 px + 2 px Griffkante (= Fensterhöhe)
        BarSize.Small => 46,      // 6 Rand + 38 Pille mit Saum (2 + 34 + 2) + 2 Rand
        BarSize.Medium => 67,     // 8 Rand + 56 Kapsel mit Saum (3 + 50 + 3) + 3 Rand
        _ => 92,                  // 10 Rand + 78 Kapsel mit Saum (4 + 70 + 4) + 4 Rand
    };

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowStyles.MakeNonActivatingToolWindow(this);
        _handle = new WindowInteropHelper(this).Handle;
        _appBar = new AppBarDocking(_handle);
        _appBar.BoundsChanged += (_, _) => Reposition();
        _appBar.FullScreenAppChanged += (_, _) => Dispatcher.BeginInvoke(RefreshAfterDisplayChange);
        _appBar.ShellRestarted += (_, _) => Dispatcher.BeginInvoke(RefreshAfterDisplayChange);
        // Nachrichten an das eigene Fenster mitlesen (HwndSource-Rückruf) – kein Windows-Hook, nichts Systemweites.
        _source = HwndSource.FromHwnd(_handle);
        _source?.AddHook(WndProc);
        WindowStyles.SetTopmost(_handle, _alwaysOnTop);
    }

    /// <summary>
    /// Nachrichten an das eigene Fenster (HwndSource-Rückruf, kein <c>SetWindowsHookEx</c>): Größenänderungen des HWND,
    /// Bildschirmwechsel und die Hinweise der Shell an die Desktop-Symbolleiste.
    /// </summary>
    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_WINDOWPOSCHANGED && lParam != 0)
        {
            OnWindowPosChanged(lParam);
        }

        if (msg == NativeMethods.WM_DISPLAYCHANGE)
        {
            _appBar?.Invalidate();
            Dispatcher.BeginInvoke(RefreshAfterDisplayChange);
        }
        else if (_appBar?.TryHandleMessage(msg, wParam, lParam) == true)
        {
            handled = true;
        }

        return 0;
    }

    /// <summary>
    /// Das HWND hat eine neue Größe (Wechsel der Vorlage, Breite des Streifens, DPI): Lage neu bestimmen und eine frei
    /// gewählte Position neu eingrenzen. Bloßes Verschieben (<c>SWP_NOSIZE</c>, auch beim Ziehen) löst nichts aus.
    /// </summary>
    private void OnWindowPosChanged(nint windowPos)
    {
        if ((unchecked((uint)Marshal.ReadInt32(windowPos, WindowPosFlagsOffset)) & NativeMethods.SWP_NOSIZE) != 0)
        {
            return;
        }

        var size = (Marshal.ReadInt32(windowPos, WindowPosWidthOffset), Marshal.ReadInt32(windowPos, WindowPosHeightOffset));
        if (size == _pixelSize)
        {
            return;
        }

        _pixelSize = size;
        _appliedCustom = null;  // andere Größe: die gespeicherte Position neu eingrenzen
        Reposition();
    }

    private void RefreshAfterDisplayChange()
    {
        RefreshLayout();
        AssertZOrder();
    }

    /// <summary>Bildschirme, Arbeitsbereich oder DPI haben sich geändert: Lage neu bestimmen und neu eingrenzen.</summary>
    private void RefreshLayout()
    {
        _appliedCustom = null;
        Reposition();
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _lastForeground = 0;
            Reposition();
            AssertZOrder();
        }
        else
        {
            _appBar?.Release();  // verborgene Leiste: Platz sofort zurückgeben
        }

        UpdateZOrderWatch();
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SystemParameters.WorkArea) or nameof(SystemParameters.VirtualScreenWidth)
            or nameof(SystemParameters.VirtualScreenHeight))
        {
            Dispatcher.BeginInvoke(RefreshLayout);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _closed = true;
        _zOrderWatch.Stop();
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        _source?.RemoveHook(WndProc);
        _appBar?.Release();
    }

    /// <summary>Setzt die Z-Reihenfolge nach der Einstellung: <c>HWND_TOPMOST</c> bzw. <c>HWND_NOTOPMOST</c>.</summary>
    private void AssertZOrder()
    {
        if (IsVisible)
        {
            WindowStyles.SetTopmost(_handle, _alwaysOnTop);
        }
    }

    /// <summary>Die Vordergrund-Prüfung läuft nur, solange die Leiste sichtbar und „stets oben“ ist.</summary>
    private void UpdateZOrderWatch()
    {
        if (IsVisible && _alwaysOnTop && _handle != 0)
        {
            if (!_zOrderWatch.IsEnabled)
            {
                _zOrderWatch.Start();
            }
        }
        else
        {
            _zOrderWatch.Stop();
        }
    }

    /// <summary>
    /// Sekundentakt (nur sichtbar und „stets oben“): zwei billige Abfragen, keine Speicheranforderung. Die Leiste wird
    /// nur nach vorn geholt, wenn ein anderes, gewöhnliches Fenster in den Vordergrund kam oder das Merkmal
    /// „stets oben“ verloren ging.
    /// </summary>
    private void CheckZOrder()
    {
        if (_handle == 0 || !IsVisible || !_alwaysOnTop)
        {
            _zOrderWatch.Stop();
            return;
        }

        var lostTopmost = !WindowStyles.IsTopmost(_handle);
        var foreground = NativeMethods.GetForegroundWindow();
        if (!lostTopmost && foreground == _lastForeground)
        {
            return;
        }

        _lastForeground = foreground;

        // Ein Fenster, das selbst „stets oben“ ist (Startmenü, Alt+Tab, Taskansicht, eigenes Kontextmenü), darf vorn
        // bleiben, solange es im Vordergrund ist; kommt danach wieder ein gewöhnliches Fenster, folgt die Leiste.
        if (!lostTopmost && (foreground == 0 || WindowStyles.IsTopmost(foreground) || ContextMenu is { IsOpen: true }))
        {
            return;
        }

        WindowStyles.SetTopmost(_handle, topmost: true);
    }

    /// <summary>
    /// Angedockt: oben mittig auf dem Hauptbildschirm. Mit „Platz freihalten“ wird der Streifen bei Windows reserviert,
    /// die Leiste sitzt genau darin (kompakter Rand ohne Schattenüberhang, <see cref="IsReservingTop"/>), und ultradünn
    /// reicht sie über die ganze Breite des Arbeitsbereichs. Ohne Reservierung bleibt der ultradünne Streifen
    /// <see cref="ThinFloatingWidth"/> breit. Die Lage wird in Gerätepixeln gesetzt – das bleibt auch bei
    /// unterschiedlicher DPI je Bildschirm exakt.
    /// </summary>
    private void DockAtTop()
    {
        if (_handle == 0)
        {
            // Vor dem ersten Zeigen: grob nach den WPF-Maßen des Hauptbildschirms.
            var area = SystemParameters.WorkArea;
            SetThinWidth(ThinFloatingWidth);
            Left = area.Left + Math.Max(0, (area.Width - ActualWidth) / 2);
            Top = area.Top;
            return;
        }

        var wantsReserve = _reservesSpace && _appBar is not null;
        var reserveNow = wantsReserve && IsVisible;
        if (!reserveNow)
        {
            _appBar?.Release();  // zuerst freigeben, damit der Arbeitsbereich unten wieder vollständig ist
        }

        var monitor = NativeMethods.MonitorFromPoint(default, NativeMethods.MONITOR_DEFAULTTOPRIMARY);
        var info = new NativeMethods.MonitorInfo { Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            SetReservingTop(false);
            SetThinWidth(ThinFloatingWidth);
            return;
        }

        var scale = MonitorScale(monitor);
        var top = info.Work.Top;
        if (reserveNow)
        {
            var reserved = _appBar!.Reserve(monitor, (int)Math.Ceiling(ReservedHeight(_size) * scale));
            if (reserved)
            {
                top = _appBar.Bounds.Top;
            }

            SetReservingTop(reserved);
        }
        else if (!wantsReserve)
        {
            SetReservingTop(false);
        }

        // Verborgen bleibt die zuletzt gezeigte Form erhalten – sonst wechselte sie beim nächsten Zeigen kurz sichtbar.
        if (SetThinWidth(IsReservingTop ? info.Work.Width / scale : ThinFloatingWidth))
        {
            // Neue Breite des Streifens: erst nach dem Layout mittig stellen, sonst zählte noch die alte Breite.
            Reposition();
            return;
        }

        if (!NativeMethods.GetWindowRect(_handle, out var rect))
        {
            return;
        }

        var left = info.Work.Left + Math.Max(0, (info.Work.Width - rect.Width) / 2);
        if (rect.Left != left || rect.Top != top)
        {
            NativeMethods.SetWindowPos(_handle, 0, left, top, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER);
        }
    }

    /// <summary>
    /// Hält eine frei gewählte Position gültig: Die Leiste wird ganz in den Arbeitsbereich des nächstgelegenen
    /// Bildschirms geschoben (z. B. nach dem Wechsel auf eine breitere Größe oder wenn ein Bildschirm fehlt).
    /// Die gespeicherte Position selbst bleibt unverändert.
    /// </summary>
    private void ClampOntoScreen()
    {
        if (_handle == 0 || !NativeMethods.GetWindowRect(_handle, out var rect))
        {
            return;
        }

        var monitor = NativeMethods.MonitorFromWindow(_handle, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new NativeMethods.MonitorInfo { Size = (uint)Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == 0 || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return;
        }

        var work = info.Work;
        var left = rect.Width >= work.Width ? work.Left : Math.Clamp(rect.Left, work.Left, work.Right - rect.Width);
        var top = rect.Height >= work.Height ? work.Top : Math.Clamp(rect.Top, work.Top, work.Bottom - rect.Height);
        if (left != rect.Left || top != rect.Top)
        {
            NativeMethods.SetWindowPos(_handle, 0, left, top, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER);
        }
    }

    /// <summary>DPI-Faktor des Bildschirms (1,0 = 96 DPI); ersatzweise der des Fensters.</summary>
    private double MonitorScale(nint monitor) =>
        NativeMethods.GetDpiForMonitor(monitor, 0, out _, out var dpiY) == 0 && dpiY > 0
            ? dpiY / 96.0
            : VisualTreeHelper.GetDpi(this).DpiScaleY;

    /// <returns><c>true</c>, wenn sich dadurch die Größe des Fensters ändert (nur bei der ultradünnen Leiste).</returns>
    private bool SetThinWidth(double width)
    {
        // Abrunden: Auf 150 % o. Ä. würde Aufrunden den Streifen um einen Pixel auf den Nachbarbildschirm schieben.
        width = Math.Max(200, Math.Floor(width));
        if (Math.Abs(ThinStripWidth - width) <= 0.5)
        {
            return false;
        }

        ThinStripWidth = width;
        return _size == BarSize.UltraThin;
    }

    /// <returns><c>true</c>, wenn sich dadurch die Höhe des Fensters ändert (alle Größen außer ultradünn).</returns>
    private bool SetReservingTop(bool value)
    {
        if (IsReservingTop == value)
        {
            return false;
        }

        IsReservingTop = value;
        return _size != BarSize.UltraThin;
    }

    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        // Schaltflächen behandeln ihre Klicks selbst; hier kommen nur Klicks auf die freie Fläche an.
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        var before = new Point(Left, Top);
        var wasDocked = _customLeft is null;
        _dragging = true;
        try
        {
            DragMove();
        }
        finally
        {
            _dragging = false;
        }

        if (Math.Abs(Left - before.X) < 2 && Math.Abs(Top - before.Y) < 2)
        {
            Reposition();  // nur geklickt, nicht gezogen
            return;
        }

        var left = Left;
        if (wasDocked && _size == BarSize.UltraThin && ThinStripWidth > ThinFloatingWidth + 0.5)
        {
            // Der bildschirmbreite Streifen schrumpft beim Loslassen auf seine feste Breite – mittig unter dem Mauszeiger.
            left += Mouse.GetPosition(this).X - (ThinFloatingWidth / 2);
        }

        (_customLeft, _customTop) = (left, Top);
        _appliedCustom = null;
        Reposition();
        PositionChosen?.Invoke(this, new Point(left, Top));
    }
}
