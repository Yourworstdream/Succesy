namespace Laternenwacht.Core.Model;

/// <summary>
/// Momentaufnahme der Benutzeraktivität.
/// Aus Datenschutzgründen wird nur der Prozessname erfasst, niemals der Fenstertitel.
/// </summary>
/// <param name="ProcessName">Name des Vordergrundprozesses ohne ".exe" oder <c>null</c>, wenn unbekannt.</param>
/// <param name="IdleTime">Zeit seit der letzten Tastatur- oder Mauseingabe.</param>
public readonly record struct ActivitySnapshot(string? ProcessName, TimeSpan IdleTime);
