using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>
/// Base commune des composants : <see cref="Class"/>, <see cref="Style"/> et attributs HTML libres,
/// avec la fusion des classes/styles de l'appelant. Même contrat que MudComponentBase.
/// </summary>
public abstract class UiComponentBase : ComponentBase
{
    /// <summary>Classes CSS supplémentaires.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Style inline supplémentaire.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Tout attribut HTML non déclaré (data-*, aria-*, id, ...).</summary>
    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes { get; set; }
}
