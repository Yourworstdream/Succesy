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
    // Frühling = Kerzenlicht (Champagner), Tauwetter = Frost (Eisblau), Winter = Moosgrau
    private static readonly Brush Spring = Freeze(new SolidColorBrush(Color.FromRgb(0xE2, 0xC4, 0x8D)));
    private static readonly Brush Thaw = Freeze(new SolidColorBrush(Color.FromRgb(0x9F, 0xD3, 0xEA)));
    private static readonly Brush Winter = Freeze(new SolidColorBrush(Color.FromRgb(0x6E, 0x7C, 0x74)));

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

/// <summary>Fehlerzustand → Fehlerrot, sonst Kerzenlicht.</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
internal sealed class ErrorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.FindResource(value is true ? "ErrorBrush" : "GoldBrush");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Zahl → Stern-Spaltenbreite (für proportionale Balken).</summary>
[ValueConversion(typeof(double), typeof(GridLength))]
internal sealed class StarConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        new GridLength(value is double d && d > 0 && !double.IsNaN(d) ? d : 0, GridUnitType.Star);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Pausiert → Wiedergabe-Symbol, sonst Pause-Symbol.</summary>
[ValueConversion(typeof(bool), typeof(Geometry))]
internal sealed class PauseGlyphConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.FindResource(value is true ? "PlayGeometry" : "PauseGeometry");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Anzahl 0 → sichtbar (Leerzustand), sonst ausgeblendet.</summary>
[ValueConversion(typeof(int), typeof(Visibility))]
internal sealed class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is 0 ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
