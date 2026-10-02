namespace Laternenwacht.Core.Model;

/// <summary>
/// Die sieben Bände der Chroniken von Narnia (in Lesereihenfolge der deutschen Ausgaben),
/// aus denen die Mahnrufe stammen können.
/// </summary>
public enum ChronicleBook
{
    /// <summary>Für jeden Mahnruf wird ein anderes Buch gewählt.</summary>
    All,
    MagiciansNephew,
    LionWitchWardrobe,
    HorseAndHisBoy,
    PrinceCaspian,
    DawnTreader,
    SilverChair,
    LastBattle,
}

public static class ChronicleBooks
{
    /// <summary>Alle konkreten Bücher (ohne <see cref="ChronicleBook.All"/>), in Lesereihenfolge.</summary>
    public static IReadOnlyList<ChronicleBook> Volumes { get; } =
    [
        ChronicleBook.MagiciansNephew,
        ChronicleBook.LionWitchWardrobe,
        ChronicleBook.HorseAndHisBoy,
        ChronicleBook.PrinceCaspian,
        ChronicleBook.DawnTreader,
        ChronicleBook.SilverChair,
        ChronicleBook.LastBattle,
    ];

    /// <summary>Deutscher Titel des Buches.</summary>
    public static string Title(ChronicleBook book) => book switch
    {
        ChronicleBook.MagiciansNephew => "Das Wunder von Narnia",
        ChronicleBook.LionWitchWardrobe => "Der König von Narnia",
        ChronicleBook.HorseAndHisBoy => "Der Ritt nach Narnia",
        ChronicleBook.PrinceCaspian => "Prinz Kaspian von Narnia",
        ChronicleBook.DawnTreader => "Die Reise auf der Morgenröte",
        ChronicleBook.SilverChair => "Der silberne Sessel",
        ChronicleBook.LastBattle => "Der letzte Kampf",
        _ => "Alle Chroniken (gemischt)",
    };

    /// <summary>Bandnummer 1–7 oder 0 für "alle".</summary>
    public static int Volume(ChronicleBook book) => book == ChronicleBook.All ? 0 : (int)book;
}
