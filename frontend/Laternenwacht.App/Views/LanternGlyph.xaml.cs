using System.Windows;
using System.Windows.Controls;

namespace Laternenwacht.App.Views;

/// <summary>Flamme (Licht) oder Schneeflocke (Frost) – das Zustandssymbol der Anwendung.</summary>
public partial class LanternGlyph : UserControl
{
    public static readonly DependencyProperty IsLitProperty = DependencyProperty.Register(
        nameof(IsLit), typeof(bool), typeof(LanternGlyph), new PropertyMetadata(true));

    public LanternGlyph() => InitializeComponent();

    public bool IsLit
    {
        get => (bool)GetValue(IsLitProperty);
        set => SetValue(IsLitProperty, value);
    }
}
