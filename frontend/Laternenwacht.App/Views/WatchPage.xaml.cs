using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Laternenwacht.Core.Model;

namespace Laternenwacht.App.Views;

/// <summary>
/// Seite „Die Wacht“ (Kapitel I): der offene Schrank vor dem Aufbruch, die Reise unterwegs (im Licht, bei der Königin,
/// bei der Rast) und das Abschlussbild der Chronik. Reine Darstellung, die Logik liegt im <see cref="ViewModels.SessionViewModel"/>.
/// </summary>
public partial class WatchPage : UserControl
{
    public WatchPage() => InitializeComponent();

    /// <summary>"Zur Chronik": wechselt im Hauptfenster auf Kapitel II.</summary>
    private void OnShowChronicle(object sender, RoutedEventArgs e) => (Window.GetWindow(this) as MainWindow)?.ShowChronicle();
}

/// <summary>
/// Zahl ↔ Auswahl: <c>true</c>, wenn der Wert dem Parameter entspricht (z. B. Dauer 25 für das Siegel "25").
/// Beim Anwählen wird der Parameter zurückgeschrieben, beim Abwählen nichts.
/// </summary>
[ValueConversion(typeof(int), typeof(bool))]
internal sealed class IntEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int number && TryParse(parameter, out var expected) && number == expected;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && TryParse(parameter, out var expected) ? expected : Binding.DoNothing;

    private static bool TryParse(object parameter, out int number) =>
        int.TryParse(System.Convert.ToString(parameter, CultureInfo.InvariantCulture), NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
}

/// <summary>
/// Gewählter Band ↔ Buchrücken: <c>true</c>, wenn der Band dem Parameter entspricht.
/// Beim Anwählen wird der Band des Buchrückens übernommen, beim Abwählen nichts.
/// </summary>
[ValueConversion(typeof(ChronicleBook), typeof(bool))]
internal sealed class BookIsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is ChronicleBook book && parameter is ChronicleBook expected && book == expected;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true && parameter is ChronicleBook book ? book : Binding.DoNothing;
}

/// <summary>Gewählter Band → "Band II · „Der König von Narnia“" bzw. "Alle sieben Chroniken, gemischt".</summary>
[ValueConversion(typeof(ChronicleBook), typeof(string))]
internal sealed class BookTitleConverter : IValueConverter
{
    private static readonly string[] Numerals = ["", "I", "II", "III", "IV", "V", "VI", "VII"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ChronicleBook book || book == ChronicleBook.All)
        {
            return "Alle sieben Chroniken, gemischt";
        }

        var volume = ChronicleBooks.Volume(book);
        var numeral = volume > 0 && volume < Numerals.Length ? Numerals[volume] : string.Empty;
        return "Band " + numeral + " · „" + ChronicleBooks.Title(book) + "“";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
