namespace WaesesUI;

/// <summary>
/// Le dialogue vu depuis le composant qu'il affiche (équivalent de <c>IMudDialogInstance</c> de MudBlazor).
/// Reçu en cascade : <c>[CascadingParameter] IUiDialogInstance Dialog { get; set; }</c>.
/// </summary>
public interface IUiDialogInstance
{
    Guid Id { get; }

    /// <summary>Titre affiché dans l'en-tête.</summary>
    string? Title { get; set; }

    /// <summary>Options en vigueur (celles du dialogue complétées par celles du provider).</summary>
    UiDialogOptions Options { get; }

    /// <summary>Change le titre.</summary>
    void SetTitle(string? title);

    /// <summary>Change les options pendant que le dialogue est ouvert (taille, position, plein écran...).</summary>
    void SetOptions(UiDialogOptions options);

    /// <summary>Ferme le dialogue avec une donnée (résultat non annulé).</summary>
    void Close(object? data = null);

    /// <summary>Ferme le dialogue avec le résultat donné.</summary>
    void Close(UiDialogResult? result);

    /// <summary>Annule le dialogue.</summary>
    void Cancel();

    /// <summary>Annule tous les dialogues ouverts, celui-ci compris.</summary>
    void CancelAll();

    /// <summary>Redessine le cadre du dialogue (en-tête, taille...).</summary>
    void StateHasChanged();
}

/// <summary>
/// Valeur en cascade fournie au composant affiché dans un dialogue :
/// <c>[CascadingParameter] UiDialogInstance Dialog { get; set; }</c> (ou <see cref="IUiDialogInstance"/>).
/// </summary>
public sealed class UiDialogInstance : IUiDialogInstance
{
    private readonly UiDialogReference _reference;
    private readonly Func<UiDialogOptions, UiDialogOptions> _resolveOptions;
    private readonly Action _changed;
    private readonly Action _cancelAll;
    private string? _title;

    internal UiDialogInstance(UiDialogReference reference, string? title, UiDialogOptions options,
        Func<UiDialogOptions, UiDialogOptions> resolveOptions, Action changed, Action cancelAll)
    {
        _reference = reference;
        _title = title;
        _resolveOptions = resolveOptions;
        _changed = changed;
        _cancelAll = cancelAll;
        Options = resolveOptions(options);
    }

    public Guid Id => _reference.Id;

    public string? Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            _changed();
        }
    }

    public UiDialogOptions Options { get; private set; }

    /// <summary>Identifiant de l'élément titre, référencé par <c>aria-labelledby</c>.</summary>
    internal string TitleId { get; } = $"dialog-title-{Guid.NewGuid():N}";

    /// <summary>Le <see cref="UiDialog"/> qui dessine l'intérieur du dialogue, s'il y en a un.</summary>
    internal UiDialog? Layout { get; private set; }

    public void SetTitle(string? title) => Title = title;

    public void SetOptions(UiDialogOptions options)
    {
        Options = _resolveOptions(options);
        _changed();
    }

    public void Close(object? data = null) => _reference.Close(data);
    public void Close(UiDialogResult? result) => _reference.Close(result);
    public void Cancel() => _reference.Cancel();
    public void CancelAll() => _cancelAll();
    public void StateHasChanged() => _changed();

    /// <summary>Un <see cref="UiDialog"/> prend en charge l'en-tête : le provider n'affiche plus le sien.</summary>
    internal void Register(UiDialog layout)
    {
        if (ReferenceEquals(Layout, layout)) return;
        Layout = layout;
        _changed();
    }

    internal void Unregister(UiDialog layout)
    {
        if (!ReferenceEquals(Layout, layout)) return;
        Layout = null;
        if (!_reference.Result.IsCompleted) _changed();
    }
}
