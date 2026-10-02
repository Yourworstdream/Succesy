using System.Windows;

namespace Laternenwacht.App.Views;

/// <summary>Hauptfenster. Die gesamte Logik liegt im <see cref="ViewModels.ShellViewModel"/>.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>Holt das Fenster in den Vordergrund, auch wenn es minimiert war.</summary>
    public void BringToFront()
    {
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Show();
        Activate();
    }

    /// <summary>Wechselt auf den Reiter "Die Chronik".</summary>
    public void ShowChronicle() => Tabs.SelectedIndex = 1;
}
