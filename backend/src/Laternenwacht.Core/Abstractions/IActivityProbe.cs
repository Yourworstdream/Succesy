using Laternenwacht.Core.Model;

namespace Laternenwacht.Core.Abstractions;

/// <summary>
/// Liefert eine Momentaufnahme dessen, was der Benutzer gerade tut.
/// Die Windows-Implementierung liest das Vordergrundfenster und die Leerlaufzeit.
/// </summary>
public interface IActivityProbe
{
    ActivitySnapshot Capture();
}
