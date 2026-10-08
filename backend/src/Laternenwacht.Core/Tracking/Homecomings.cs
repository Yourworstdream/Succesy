using System.Collections.Frozen;
using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Tracking;

/// <summary>Wie lange der Benutzer fort war.</summary>
public enum AbsenceLength
{
    /// <summary>Unter 2 Minuten.</summary>
    Brief,

    /// <summary>2 bis unter 10 Minuten.</summary>
    Moderate,

    /// <summary>10 Minuten und mehr.</summary>
    Extended,
}

/// <summary>Ein ermutigendes Wort eines Bewohners des Reiches (Lob oder Willkommensgruß).</summary>
/// <param name="Text">Der Text.</param>
/// <param name="Speaker">Wer spricht, z. B. "Riepiepich".</param>
/// <param name="Book">Aus welchem Buch der Chroniken der Sprecher stammt.</param>
public sealed record Encouragement(string Text, string Speaker, ChronicleBook Book);

/// <summary>
/// "Willkommen zurück": Grüße der Bewohner des Reiches, wenn man nach einer Ablenkung
/// zur Arbeit zurückkehrt. Je Buch ein Gruß für kurze, mittlere und lange Abwesenheit.
/// Alle Texte sind eigene Formulierungen.
/// </summary>
public static class Homecomings
{
    public static readonly TimeSpan MediumFrom = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan LongFrom = TimeSpan.FromMinutes(10);

    private static readonly FrozenDictionary<ChronicleBook, (string Text, string Speaker)[]> Greetings =
        new Dictionary<ChronicleBook, (string Text, string Speaker)[]>
        {
            [ChronicleBook.MagiciansNephew] =
            [
                ("Schon zurück aus dem Wald zwischen den Welten? Gut gemacht – der gelbe Ring bleibt in der Tasche.", "Polly"),
                ("Da bist du ja! Ich dachte schon, Onkel Andrew hätte dich für eines seiner Experimente eingespannt.", "Digory"),
                ("Endlich! Die Welt wurde inzwischen fertig gesungen – aber für deine Aufgabe ist noch Platz.", "das geflügelte Pferd"),
            ],
            [ChronicleBook.LionWitchWardrobe] =
            [
                ("Du bist aufgestanden, bevor die Schachtel leer war. Edmund hat dafür viel länger gebraucht.", "Lucy"),
                ("Da bist du ja! Ich hab den Topf vom Feuer genommen, damit nichts anbrennt. Setz dich, das Essen ist noch warm.", "Frau Biber"),
                ("Edmund kam auch zurück, und keiner hat ihm den Umweg vorgehalten. Komm, wir gehen zusammen weiter.", "Peter"),
            ],
            [ChronicleBook.HorseAndHisBoy] =
            [
                ("Na also. Ein kurzer Galopp ins Abseits, und schon wieder auf der Straße nach Norden.", "Bree"),
                ("Da bist du! Hwin wollte schon umkehren und dich holen.", "Aravis"),
                ("Narnia und der Norden! Beinahe hätten wir Archenland ohne dich erreicht.", "Bree"),
            ],
            [ChronicleBook.PrinceCaspian] =
            [
                ("Bei meinem Bart, schon zurück? Dann weiter – die Telmarer schlafen nicht.", "Trumpkin"),
                ("Willkommen zurück, Freund. Der Kriegsrat wollte ohne dich nichts beschließen.", "Trüffeljäger"),
                ("Kaspian hat lange auf dich gewartet. Wenn es nach mir ginge, wären wir ohne dich losgezogen.", "Riepiepich"),
            ],
            [ChronicleBook.DawnTreader] =
            [
                ("Leinen los – du bist wieder an Bord!", "Kapitän Drinian"),
                ("Ich schreibe es in mein Tagebuch: Du warst fort. Jetzt bist du wieder da. Höchst spannend.", "Eustachius"),
                ("Kaspian hat lange auf dich gewartet. Wenn es nach mir ginge, wären wir ohne dich losgesegelt.", "Riepiepich"),
            ],
            [ChronicleBook.SilverChair] =
            [
                ("Zurück? Hm. Vermutlich nur für kurz. Aber schön, dass du da bist.", "ein griesgrämiger Moorbewohner"),
                ("Gut so. Denk an die Zeichen – und schau nicht wieder zurück.", "Jill"),
                ("Du warst so lange fort, ich dachte schon, die Dame im grünen Gewand hätte dich ins Unterland gelockt.", "Eustachius"),
            ],
            [ChronicleBook.LastBattle] =
            [
                ("Gut gemacht – nicht jede Stalltür muss man öffnen. Schön, dass du zurück bist.", "König Tirian"),
                ("Willkommen zurück. Ich habe schon unruhig mit dem Huf gescharrt.", "Juwel, das Einhorn"),
                ("Da bist du endlich! Wir haben die Stellung für dich gehalten – jetzt reiten wir gemeinsam weiter.", "Juwel, das Einhorn"),
            ],
        }.ToFrozenDictionary();

    public static AbsenceLength Classify(TimeSpan absence) =>
        absence >= LongFrom ? AbsenceLength.Extended : absence >= MediumFrom ? AbsenceLength.Moderate : AbsenceLength.Brief;

    /// <summary>Wählt den passenden Gruß; bei <see cref="ChronicleBook.All"/> wechselt das Buch mit dem Startwert.</summary>
    public static Encouragement For(ChronicleBook book, TimeSpan absence, int seed = 0)
    {
        if (!Enum.IsDefined(book))
        {
            throw new ArgumentOutOfRangeException(nameof(book));
        }

        var source = book == ChronicleBook.All
            ? ChronicleBooks.Volumes[(int)((uint)seed % (uint)ChronicleBooks.Volumes.Count)]
            : book;
        var (text, speaker) = Greetings[source][(int)Classify(absence)];
        return new Encouragement(text, speaker, source);
    }
}
