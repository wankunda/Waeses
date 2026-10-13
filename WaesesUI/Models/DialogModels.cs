using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>Position d'un dialogue dans la fenêtre (équivalent de <c>DialogPosition</c> de MudBlazor).</summary>
public enum UiDialogPosition
{
    Center,
    TopCenter,
    TopLeft,
    TopRight,
    CenterLeft,
    CenterRight,
    BottomCenter,
    BottomLeft,
    BottomRight
}

/// <summary>Élément qui reçoit le focus à l'ouverture d'un dialogue (équivalent de <c>DefaultFocus</c> de MudBlazor).</summary>
public enum UiDefaultFocus
{
    /// <summary>Premier élément focalisable du dialogue (bouton, champ...).</summary>
    FirstChild,
    /// <summary>Dernier élément focalisable du dialogue (souvent le bouton de validation).</summary>
    LastChild,
    /// <summary>La boîte du dialogue elle-même.</summary>
    Element,
    /// <summary>Aucun déplacement du focus.</summary>
    None
}

/// <summary>
/// Options d'affichage d'un dialogue (équivalent de <c>DialogOptions</c> de MudBlazor).
/// Une option laissée à <c>null</c> prend la valeur réglée sur <see cref="UiDialogProvider"/>.
/// </summary>
public class UiDialogOptions
{
    /// <summary>Position du dialogue dans la fenêtre.</summary>
    public UiDialogPosition? Position { get; set; }

    /// <summary>Largeur maximale du dialogue (équivalent de <c>MaxWidth</c>).</summary>
    public UiSize? Size { get; set; }

    /// <summary>Occupe tout l'écran, sans marge ni coins arrondis.</summary>
    public bool? FullScreen { get; set; }

    /// <summary>Masque l'en-tête (titre et bouton de fermeture).</summary>
    public bool? NoHeader { get; set; }

    /// <summary>Affiche le bouton de fermeture dans l'en-tête (équivalent de <c>CloseButton</c>).</summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>Annule le dialogue au clic sur le fond assombri (équivalent de <c>BackdropClick</c>).</summary>
    public bool? CloseOnBackdropClick { get; set; }

    /// <summary>Annule le dialogue avec la touche Échap (équivalent de <c>CloseOnEscapeKey</c>).</summary>
    public bool? CloseOnEscape { get; set; }

    /// <summary>Élément qui reçoit le focus à l'ouverture.</summary>
    public UiDefaultFocus? DefaultFocus { get; set; }

    /// <summary>Classes CSS ajoutées au fond assombri (équivalent de <c>BackgroundClass</c>).</summary>
    public string? BackdropClass { get; set; }

    /// <summary>Classes CSS ajoutées à la boîte du dialogue.</summary>
    public string? Class { get; set; }

    /// <summary>Style inline ajouté à la boîte du dialogue.</summary>
    public string? Style { get; set; }

    /// <summary>Nouvelles options : chaque valeur non renseignée ici est prise dans <paramref name="defaults"/>.</summary>
    public UiDialogOptions MergeWith(UiDialogOptions defaults) => new()
    {
        Position = Position ?? defaults.Position,
        Size = Size ?? defaults.Size,
        FullScreen = FullScreen ?? defaults.FullScreen,
        NoHeader = NoHeader ?? defaults.NoHeader,
        ShowCloseButton = ShowCloseButton ?? defaults.ShowCloseButton,
        CloseOnBackdropClick = CloseOnBackdropClick ?? defaults.CloseOnBackdropClick,
        CloseOnEscape = CloseOnEscape ?? defaults.CloseOnEscape,
        DefaultFocus = DefaultFocus ?? defaults.DefaultFocus,
        BackdropClass = BackdropClass ?? defaults.BackdropClass,
        Class = Class ?? defaults.Class,
        Style = Style ?? defaults.Style
    };
}

/// <summary>Paramètres passés au composant affiché dans le dialogue, par nom de paramètre.</summary>
public class UiDialogParameters : Dictionary<string, object?>
{
    public UiDialogParameters With(string name, object? value)
    {
        this[name] = value;
        return this;
    }

    /// <summary>Valeur du paramètre <paramref name="name"/>, ou <c>default</c> s'il est absent ou d'un autre type.</summary>
    public T? Get<T>(string name) => TryGetValue(name, out var value) && value is T typed ? typed : default;
}

/// <summary>
/// Paramètres typés (équivalent de <c>DialogParameters&lt;T&gt;</c> de MudBlazor) : le nom du paramètre est vérifié à la compilation.
/// <code>new UiDialogParameters&lt;EditNameDialog&gt;().Add(x => x.Name, "Alpha")</code>
/// </summary>
public class UiDialogParameters<TComponent> : UiDialogParameters where TComponent : IComponent
{
    public UiDialogParameters<TComponent> Add<TValue>(Expression<Func<TComponent, TValue>> parameter, TValue value)
    {
        if (parameter.Body is not MemberExpression member)
        {
            throw new ArgumentException("L'expression doit désigner une propriété du composant, par exemple x => x.Name.", nameof(parameter));
        }

        this[member.Member.Name] = value;
        return this;
    }
}

/// <summary>Résultat d'un dialogue : annulé, ou fermé avec une donnée optionnelle (équivalent de <c>DialogResult</c>).</summary>
public sealed class UiDialogResult
{
    private UiDialogResult(bool canceled, object? data, Type? dataType)
    {
        Canceled = canceled;
        Data = data;
        DataType = dataType;
    }

    public bool Canceled { get; }
    public object? Data { get; }

    /// <summary>Type de la donnée renvoyée (<c>null</c> si aucune donnée).</summary>
    public Type? DataType { get; }

    public static UiDialogResult Ok(object? data = null) => new(false, data, data?.GetType());
    public static UiDialogResult Ok<T>(T data) => new(false, data, typeof(T));
    public static UiDialogResult Cancel() => new(true, null, null);

    /// <summary>Donnée typée, ou <c>default</c> si le dialogue est annulé ou de type différent.</summary>
    public T? GetData<T>() => Data is T value ? value : default;
}
