using System.Windows;

namespace Laternenwacht.App.Views;

/// <summary>Hauptfenster mit eigener Fensterleiste. Die gesamte Logik liegt im <see cref="ViewModels.ShellViewModel"/>.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Maximiert ragt ein Fenster mit eigener Leiste um den Rahmen über den Bildschirm hinaus – ausgleichen.
        StateChanged += (_, _) => WindowRoot.Margin = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(0);
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

    /// <summary>Wechselt auf die Seite "Chronik".</summary>
    public void ShowChronicle() => Tabs.SelectedIndex = 1;

    private void OnMinimize(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            SystemCommands.RestoreWindow(this);
        }
        else
        {
            SystemCommands.MaximizeWindow(this);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
