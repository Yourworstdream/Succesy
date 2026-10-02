using Laternenwacht.Core.Model;
using Laternenwacht.Core.Tracking;

namespace Laternenwacht.App.Services;

/// <summary>
/// Erzählstimme der Anwendung. Alle Texte sind eigene Formulierungen,
/// inspiriert von der Atmosphäre klassischer Fantasy-Chroniken:
/// Laternenpfahl im Schneewald, ewiger Winter, der wiederkehrende Frühling.
/// </summary>
internal static class Lore
{
    private static readonly string[] Proverbs =
    [
        "Ein Licht im Schnee genügt, um den Weg nach Hause zu finden.",
        "Der Winter endet nicht an einem Tag – aber er endet.",
        "Wer die Laterne hütet, hütet den Pfad.",
        "Nicht jede Tür im Schrank führt dorthin, wo du hinwillst.",
        "Der Löwe brüllt nicht für die Säumigen, sondern für die Wachen.",
        "Weiter hinauf und weiter hinein – Schritt für Schritt.",
        "Auch die längste Nacht im Wald kennt einen Morgen.",
    ];

    public static string Proverb(int seed) => Proverbs[Math.Abs(seed % Proverbs.Length)];

    public static (string Headline, string Detail) Describe(FocusSession? session, ActivitySnapshot? snapshot)
    {
        if (session is null)
        {
            return ("Die Laterne wartet", "Entzünde sie, um deine Wacht zu beginnen.");
        }

        return session.Phase switch
        {
            SessionPhase.Paused => ("Rast am Feuer", "Die Zeit im Reich steht still, bis du weiterziehst."),
            SessionPhase.Aborted => ("Die Wacht wurde abgebrochen", "Auch ein abgebrochener Weg ist in der Chronik verzeichnet."),
            SessionPhase.Completed => Completed(RealmMoods.FromFrost(session.FrostRatio)),
            _ => session.CurrentState switch
            {
                ActivityState.Distracted => ("Der Winter kriecht heran",
                    session.CurrentProcess is { Length: > 0 } p ? $"Eine Verlockung ruft: {p}" : "Eine Verlockung ruft."),
                ActivityState.Away => ("Die Laterne wacht allein",
                    $"Keine Spur im Schnee seit {TimeFormat.Clock(snapshot?.IdleTime ?? TimeSpan.Zero)}."),
                _ => ("Die Laterne brennt hell", "Du wandelst auf dem rechten Pfad."),
            },
        };
    }

    public static (string Headline, string Detail) Completed(RealmMood mood) => mood switch
    {
        RealmMood.Spring => ("Der Frühling ist gekommen!", "Der Schnee schmilzt, die Bäche singen – eine würdige Wacht."),
        RealmMood.Thaw => ("Das Eis bricht", "Tauwetter im Reich – ein ehrbarer Sieg über den Winter."),
        _ => ("Noch herrscht Winter", "Doch jede vollendete Wacht bringt den Frühling näher."),
    };

    public static string MoodName(RealmMood mood) => mood switch
    {
        RealmMood.Spring => "Frühling",
        RealmMood.Thaw => "Tauwetter",
        _ => "Winter",
    };

    public static string OutcomeName(SessionPhase phase) => phase switch
    {
        SessionPhase.Completed => "Vollendet",
        SessionPhase.Aborted => "Abgebrochen",
        _ => "Unterwegs",
    };
}
