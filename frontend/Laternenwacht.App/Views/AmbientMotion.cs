using System.Windows;

namespace Laternenwacht.App.Views;

/// <summary>
/// Hält dekorative Endlos-Animationen nur dann am Laufen, wenn sie jemand ansieht:
/// Das Element ist sichtbar, sein Fenster weder minimiert noch im Hintergrund, und die
/// Windows-Einstellung "Animationen anzeigen" ist eingeschaltet. Andernfalls ruhen sie –
/// und kosten weder Prozessor noch Grafikkarte.
/// </summary>
/// <remarks>
/// WPF berechnet laufende Animationen auch für verdeckte oder minimierte Fenster weiter.
/// Während einer Wacht arbeitet man in anderen Programmen; dann ruht die Szene vollständig.
/// </remarks>
internal sealed class AmbientMotion
{
    private readonly FrameworkElement _owner;
    private readonly Action<bool> _apply;
    private Window? _window;
    private bool _running;

    /// <param name="owner">Element, dessen Sichtbarkeit zählt.</param>
    /// <param name="apply">Wird mit <c>true</c> (weiterlaufen) bzw. <c>false</c> (anhalten) aufgerufen – nur bei Änderungen.</param>
    public AmbientMotion(FrameworkElement owner, Action<bool> apply)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        owner.Loaded += (_, _) => Attach();
        owner.Unloaded += (_, _) => Detach();
        owner.IsVisibleChanged += (_, _) => Update();
    }

    private void Attach()
    {
        Detach();
        _window = Window.GetWindow(_owner);
        if (_window is not null)
        {
            _window.Activated += OnWindowChanged;
            _window.Deactivated += OnWindowChanged;
            _window.StateChanged += OnWindowChanged;
        }

        Update();
    }

    private void Detach()
    {
        if (_window is not null)
        {
            _window.Activated -= OnWindowChanged;
            _window.Deactivated -= OnWindowChanged;
            _window.StateChanged -= OnWindowChanged;
            _window = null;
        }

        Update();
    }

    private void OnWindowChanged(object? sender, EventArgs e) => Update();

    private void Update()
    {
        var shouldRun = SystemParameters.ClientAreaAnimation
            && _owner.IsVisible
            && _window is { IsActive: true, WindowState: not WindowState.Minimized };

        if (shouldRun != _running)
        {
            _running = shouldRun;
            _apply(shouldRun);
        }
    }
}
