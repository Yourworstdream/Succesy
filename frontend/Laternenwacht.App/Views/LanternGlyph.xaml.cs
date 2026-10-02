using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Laternenwacht.App.Views;

/// <summary>Animierte Laterne – das Leitsymbol der Anwendung.</summary>
public partial class LanternGlyph : UserControl
{
    public static readonly DependencyProperty IsLitProperty = DependencyProperty.Register(
        nameof(IsLit), typeof(bool), typeof(LanternGlyph), new PropertyMetadata(true, OnIsLitChanged));

    private readonly Storyboard _flicker;

    public LanternGlyph()
    {
        InitializeComponent();
        _flicker = (Storyboard)Resources["Flicker"];
        Loaded += (_, _) => UpdateAnimation();
        Unloaded += (_, _) => _flicker.Stop(this);
    }

    public bool IsLit
    {
        get => (bool)GetValue(IsLitProperty);
        set => SetValue(IsLitProperty, value);
    }

    private static void OnIsLitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((LanternGlyph)d).UpdateAnimation();

    private void UpdateAnimation()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (IsLit)
        {
            _flicker.Begin(this, isControllable: true);
        }
        else
        {
            _flicker.Stop(this);
            Halo.Opacity = 0.15;
        }
    }
}
