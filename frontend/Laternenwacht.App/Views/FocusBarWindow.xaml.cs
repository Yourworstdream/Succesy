using System.Windows;
using System.Windows.Input;

namespace Laternenwacht.App.Views;

/// <summary>
/// Randlose, stets sichtbare Kapsel. Standardmäßig sitzt sie mittig am oberen Bildschirmrand;
/// per Maus lässt es sich frei verschieben, die Position wird gespeichert.
/// Es stiehlt beim Anklicken nicht den Fokus – sonst würde es selbst die Messung verfälschen.
/// </summary>
public partial class FocusBarWindow : Window
{
    private double? _customLeft;
    private double? _customTop;
    private bool _dragging;

    public FocusBarWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowStyles.MakeNonActivatingToolWindow(this);
        SizeChanged += (_, _) => Reposition();
        Loaded += (_, _) => Reposition();
        MouseLeftButtonDown += OnDragStart;
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(SystemParameters.WorkArea) or nameof(SystemParameters.VirtualScreenWidth))
            {
                Dispatcher.BeginInvoke(Reposition);
            }
        };
    }

    /// <summary>Der Benutzer hat die Leiste an eine neue Stelle gezogen (geräteunabhängige Pixel).</summary>
    public event EventHandler<Point>? PositionChosen;

    /// <summary>Setzt eine frei gewählte Position oder – mit <c>null</c> – das Andocken am oberen Rand.</summary>
    public void ApplyPosition(double? left, double? top)
    {
        (_customLeft, _customTop) = left is not null && top is not null ? (left, top) : (null, null);
        Reposition();
    }

    /// <summary>Stellt die Leiste an ihre gespeicherte Position bzw. dockt sie oben an.</summary>
    public void Reposition()
    {
        if (_dragging)
        {
            return;
        }

        if (_customLeft is { } left && _customTop is { } top && IsOnScreen(left, top))
        {
            Left = left;
            Top = top;
            return;
        }

        // Ohne eigene Position – oder wenn der Bildschirm von damals fehlt – oben zentriert andocken.
        var area = SystemParameters.WorkArea;
        Left = area.Left + Math.Max(0, (area.Width - ActualWidth) / 2);
        Top = area.Top;
    }

    private void OnDragStart(object sender, MouseButtonEventArgs e)
    {
        // Schaltflächen behandeln ihre Klicks selbst; hier kommen nur Klicks auf die freie Fläche an.
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        var before = new Point(Left, Top);
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

        (_customLeft, _customTop) = (Left, Top);
        PositionChosen?.Invoke(this, new Point(Left, Top));
    }

    private bool IsOnScreen(double left, double top)
    {
        // Mindestens ein gut greifbarer Streifen der Leiste muss auf dem virtuellen Bildschirm liegen.
        var width = ActualWidth > 0 ? ActualWidth : Width;
        return left + width - 80 >= SystemParameters.VirtualScreenLeft
            && left + 80 <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
            && top >= SystemParameters.VirtualScreenTop - 10
            && top + 30 <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
    }
}
