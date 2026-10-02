namespace Laternenwacht.Core.Model;

/// <summary>Strategie, nach der unbekannte Programme bewertet werden.</summary>
public enum ClassificationMode
{
    /// <summary>Nur die "Gefährten" (Erlaubnisliste) zählen als Fokus – alles andere ist Ablenkung.</summary>
    AllowList,

    /// <summary>Nur die "Verlockungen" (Sperrliste) zählen als Ablenkung – alles andere ist Fokus.</summary>
    BlockList,
}
