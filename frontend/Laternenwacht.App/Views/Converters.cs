using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Views;

/// <summary>true → Collapsed, false → Visible.</summary>
[ValueConversion(typeof(bool), typeof(Visibility))]
internal sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Collapsed;
}

/// <summary>Leere Zeichenkette oder null → Collapsed.</summary>
[ValueConversion(typeof(string), typeof(Visibility))]
internal sealed class TextToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Jahreszeit → Akzentfarbe.</summary>
[ValueConversion(typeof(RealmMood), typeof(Brush))]
internal sealed class MoodToBrushConverter : IValueConverter
{
    private static readonly Brush Spring = Freeze(new SolidColorBrush(Color.FromRgb(0x6E, 0x9E, 0x55)));
    private static readonly Brush Thaw = Freeze(new SolidColorBrush(Color.FromRgb(0x4F, 0x8F, 0xA8)));
    private static readonly Brush Winter = Freeze(new SolidColorBrush(Color.FromRgb(0x5A, 0x6B, 0x86)));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        RealmMood.Spring => Spring,
        RealmMood.Thaw => Thaw,
        _ => Winter,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}

/// <summary>Fehlerzustand → Karmesin, sonst Frühlingsgrün.</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
internal sealed class ErrorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.FindResource(value is true ? "CrimsonBrush" : "SpringBrush");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
