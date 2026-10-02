namespace Laternenwacht.Core.Tracking;

/// <summary>
/// Die "Jahreszeit" des Reiches – eine anschauliche Verdichtung des Ablenkungsanteils.
/// </summary>
public enum RealmMood
{
    /// <summary>Kaum Ablenkung: Der Frühling blüht.</summary>
    Spring,

    /// <summary>Spürbare Ablenkung: Tauwetter, das Eis knirscht.</summary>
    Thaw,

    /// <summary>Viel Ablenkung: Ewiger Winter.</summary>
    Winter,
}

public static class RealmMoods
{
    public const double ThawThreshold = 0.10;
    public const double WinterThreshold = 0.25;

    /// <summary>Bestimmt die Jahreszeit aus dem Frostanteil (Ablenkung / gemessene Zeit).</summary>
    public static RealmMood FromFrost(double frostRatio) => frostRatio switch
    {
        double.NaN => RealmMood.Spring,
        < ThawThreshold => RealmMood.Spring,
        < WinterThreshold => RealmMood.Thaw,
        _ => RealmMood.Winter,
    };
}
