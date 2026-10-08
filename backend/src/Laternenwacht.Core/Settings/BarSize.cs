namespace Laternenwacht.Core.Settings;

/// <summary>
/// Größe der Fokusleiste am oberen Bildschirmrand.
/// </summary>
/// <remarks>
/// Die Namen werden als Text in der Einstellungsdatei gespeichert – <b>nicht umbenennen</b>, sonst fallen
/// gespeicherte Einstellungen auf die Standardwerte zurück. Neue Größen nur anhängen.
/// </remarks>
public enum BarSize
{
    /// <summary>Haarfeiner Streifen (ca. 6 px) mit Fortschritt, Stationen und Honig; Einzelheiten im Tooltip.</summary>
    UltraThin,

    /// <summary>Schmale Pille (ca. 300 × 34 px): Zustandszeichen, Restzeit und Frost.</summary>
    Small,

    /// <summary>Mittlere Kapsel (ca. 560 × 50 px): Medaillon, Restzeit, Kurzstatus, Wegfaden, Frost und Knöpfe.</summary>
    Medium,

    /// <summary>Die volle Kapsel (ca. 776 × 70 px) – die ursprüngliche Leiste.</summary>
    Large,
}
