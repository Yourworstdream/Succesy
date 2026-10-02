namespace Laternenwacht.Core.Model;

/// <summary>Bewertung eines einzelnen Messzeitpunkts.</summary>
public enum ActivityState
{
    /// <summary>Der Benutzer arbeitet mit einem erlaubten Programm – die Laterne brennt.</summary>
    Focused,

    /// <summary>Der Benutzer ist abgelenkt – der Frost kriecht heran.</summary>
    Distracted,

    /// <summary>Keine Eingaben über die Leerlaufschwelle hinaus – die Laterne wacht allein.</summary>
    Away,
}
