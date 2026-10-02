namespace Laternenwacht.Core.Integrity;

/// <summary>
/// Eine Zeile im Sitzungsjournal. <see cref="Payload"/> enthält den serialisierten
/// <see cref="Model.SessionRecord"/> als exakten Text, damit die Prüfsumme byte-genau
/// nachgerechnet werden kann.
/// </summary>
/// <param name="Seq">Fortlaufende Nummer, beginnend bei 1.</param>
/// <param name="Prev">MAC des Vorgängers (Hash-Kette); beim ersten Eintrag das Genesis-Siegel.</param>
/// <param name="Payload">JSON des Sitzungsergebnisses.</param>
/// <param name="Mac">HMAC-SHA256 über Seq, Prev und Payload (hexadezimal).</param>
internal sealed record JournalEntry(long Seq, string Prev, string Payload, string Mac);

/// <summary>
/// Separat gespeicherter "Anker" auf den letzten Eintrag.
/// Er macht auch das Abschneiden der letzten Einträge erkennbar.
/// </summary>
internal sealed record JournalAnchor(long Seq, string Mac, string AnchorMac);
