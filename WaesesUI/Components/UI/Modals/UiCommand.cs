namespace WaesesUI;

/// <summary>Commande affichée dans <see cref="UiCommandPalette"/>.</summary>
public sealed record UiCommand
{
    /// <summary>Libellé visible de la commande.</summary>
    public required string Label { get; init; }

    /// <summary>Texte descriptif facultatif affiché sous le libellé.</summary>
    public string? Description { get; init; }

    /// <summary>Groupe facultatif de la commande.</summary>
    public string? Group { get; init; }

    /// <summary>Icône facultative, par exemple un glyphe ou un court texte.</summary>
    public string? Icon { get; init; }

    /// <summary>Raccourci facultatif affiché avec la commande.</summary>
    public string? Shortcut { get; init; }

    /// <summary>Indique si la commande ne peut pas être sélectionnée.</summary>
    public bool Disabled { get; init; }

    /// <summary>Action asynchrone exécutée lorsque la commande est sélectionnée.</summary>
    public required Func<Task> Execute { get; init; }
}
