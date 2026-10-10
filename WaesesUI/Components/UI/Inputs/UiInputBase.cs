using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace WaesesUI;

/// <summary>
/// Base commune des champs de saisie (équivalent de MudBaseInput) : paramètres partagés, collecte des erreurs,
/// validation personnalisée branchée sur <see cref="EditContext"/>, attributs ARIA et saisie différée.
/// </summary>
public abstract class UiInputBase<TValue> : InputBase<TValue>
{
    private ValidationMessageStore? _store;
    private EditContext? _subscribedContext;
    private bool _initialCaptured;
    private bool _touched;
    private TValue? _initialValue;
    private CancellationTokenSource? _debounce;

    /// <summary>Préfixe de l'identifiant généré (<c>select-…</c>, <c>textarea-…</c>).</summary>
    protected virtual string IdPrefix => "input";

    /// <summary>Identifiant de l'élément ; généré automatiquement si absent.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Libellé affiché au-dessus du champ.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Texte d'aide sous le champ, remplacé par le message d'erreur quand il y en a un.</summary>
    [Parameter] public string? HelperText { get; set; }

    /// <summary>Message d'erreur forcé par l'appelant (en plus de ceux de la validation).</summary>
    [Parameter] public string? ErrorMessage { get; set; }

    /// <summary>Force l'état d'erreur visuel, avec ou sans message.</summary>
    [Parameter] public bool Error { get; set; }
    /// <summary>Marque le champ comme obligatoire (astérisque et <c>aria-required</c>). Un attribut <c>[Required]</c> sur la propriété liée a le même effet.</summary>
    [Parameter] public bool Required { get; set; }
    /// <summary>Désactive le champ.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Rend le champ lisible mais non modifiable.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>Classes CSS supplémentaires, posées sur la racine du champ.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Style inline supplémentaire, posé sur la racine du champ.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Règle de validation personnalisée : renvoie le message d'erreur, ou <c>null</c> / vide si la valeur est valide.
    /// Dans un <c>EditForm</c> elle participe à <c>Validate()</c> et à la soumission ; seule, elle s'applique dès la première modification.
    /// </summary>
    [Parameter] public Func<TValue?, string?>? Validation { get; set; }

    /// <summary>Apparence du champ : contour (défaut), fond teinté ou soulignement seul.</summary>
    [Parameter] public UiVariant? Variant { get; set; }

    /// <summary>Couleur de mise en avant (focus). Défaut : couleur primaire du thème.</summary>
    [Parameter] public UiColor? Color { get; set; }

    /// <summary>Classes d'apparence (<see cref="Variant"/>, <see cref="Color"/>) à poser sur la racine du champ.</summary>
    protected string FieldClass => UiStyle.FieldClasses(Variant, Color);

    /// <summary>Classe de couleur seule, pour les contrôles sans apparence Filled/Text (case à cocher, interrupteur, curseur).</summary>
    protected string ColorClass => Color.HasValue ? Color.Value.ToCssClass() : string.Empty;

    /// <summary>Met à jour la valeur à chaque frappe (vrai) ou seulement à la validation du champ (faux).</summary>
    [Parameter] public bool Immediate { get; set; } = true;

    /// <summary>Attend ce délai (ms) sans frappe avant de mettre à jour la valeur ; 0 = aucun délai.</summary>
    [Parameter] public int DebounceInterval { get; set; }

    protected string ErrorId => $"{Id}-error";
    protected string HelperId => $"{Id}-helper";
    protected bool HasError => Error || GetErrors().Any();
    protected string? AriaInvalid => HasError ? "true" : null;
    protected string? DescribedBy => HasError && GetErrors().Any() ? ErrorId : (!string.IsNullOrEmpty(HelperText) ? HelperId : null);
    protected bool IsRequired => Required || IsRequiredByAttribute();

    protected IEnumerable<string> GetErrors()
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            yield return ErrorMessage;
        }

        if (EditContext != null && ValueExpression != null)
        {
            foreach (var message in EditContext.GetValidationMessages(FieldIdentifier))
            {
                yield return message;
            }
        }
        else if (Validation != null && _touched)
        {
            var message = Validation(CurrentValue);
            if (!string.IsNullOrEmpty(message))
            {
                yield return message;
            }
        }
    }

    protected string GetFirstError() => GetErrors().FirstOrDefault() ?? string.Empty;

    /// <summary>Vrai si la propriété liée porte <c>[Required]</c> (affiche l'astérisque).</summary>
    protected bool IsRequiredByAttribute()
    {
        if (EditContext != null && ValueExpression?.Body is MemberExpression member)
        {
            return member.Member.GetCustomAttributes(typeof(RequiredAttribute), true).Length > 0;
        }
        return false;
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Id ??= $"{IdPrefix}-{Guid.NewGuid():N}";
        if (EditContext != null && ValueExpression != null)
        {
            _subscribedContext = EditContext;
            _store = new ValidationMessageStore(EditContext);
            EditContext.OnValidationRequested += HandleValidationRequested;
            EditContext.OnFieldChanged += HandleFieldChanged;
        }
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!_initialCaptured)
        {
            _initialValue = Value;
            _initialCaptured = true;
        }
        else if (!EqualityComparer<TValue?>.Default.Equals(Value, _initialValue))
        {
            _touched = true;
        }
    }

    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs e) => RunValidation();

    private void HandleFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        if (e.FieldIdentifier.Equals(FieldIdentifier))
        {
            RunValidation();
        }
    }

    private void RunValidation()
    {
        if (_store == null || EditContext == null) return;

        _store.Clear(FieldIdentifier);
        var message = Validation?.Invoke(CurrentValue);
        if (!string.IsNullOrEmpty(message))
        {
            _store.Add(FieldIdentifier, message);
        }
        EditContext.NotifyValidationStateChanged();
    }

    /// <summary>
    /// Traite une saisie texte : <paramref name="isInput"/> pour l'évènement <c>input</c> (frappe), faux pour <c>change</c>.
    /// Applique <see cref="Immediate"/> et <see cref="DebounceInterval"/>.
    /// </summary>
    protected async Task HandleTextEventAsync(string? value, bool isInput)
    {
        _debounce?.Cancel();
        _debounce?.Dispose();
        _debounce = null;

        if (isInput)
        {
            if (!Immediate) return;

            if (DebounceInterval > 0)
            {
                var cts = _debounce = new CancellationTokenSource();
                try
                {
                    await Task.Delay(DebounceInterval, cts.Token);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        CurrentValueAsString = value;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounce?.Cancel();
            _debounce?.Dispose();
            if (_subscribedContext != null)
            {
                _subscribedContext.OnValidationRequested -= HandleValidationRequested;
                _subscribedContext.OnFieldChanged -= HandleFieldChanged;
            }
        }
        base.Dispose(disposing);
    }
}
