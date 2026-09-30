using System.Windows;
using System.Windows.Interop;
using Laternenwacht.App.Services.Native;

namespace Laternenwacht.App.Views;

/// <summary>
/// Randloses, stets sichtbares Banner am oberen Bildschirmrand.
/// Es stiehlt beim Anklicken nicht den Fokus – sonst würde es selbst die Messung verfälschen.
/// </summary>
public partial class FocusBarWindow : Window
{
    public FocusBarWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyToolWindowStyle();
        SizeChanged += (_, _) => DockToTop();
        Loaded += (_, _) => DockToTop();
        SystemParameters.StaticPropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SystemParameters.WorkArea))
            {
                Dispatcher.BeginInvoke(DockToTop);
            }
        };
    }

    /// <summary>Zentriert die Leiste am oberen Rand des Arbeitsbereichs des Hauptbildschirms.</summary>
    public void DockToTop()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Left + Math.Max(0, (area.Width - ActualWidth) / 2);
        Top = area.Top;
    }

    private void ApplyToolWindowStyle()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var style = NativeMethods.GetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWL_EXSTYLE,
            style | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
    }
}
