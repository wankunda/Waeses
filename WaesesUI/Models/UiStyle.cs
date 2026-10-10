namespace WaesesUI;

/// <summary>Classes CSS communes d'apparence (<see cref="UiVariant"/>) et de couleur (<see cref="UiColor"/>) des champs.</summary>
public static class UiStyle
{
    /// <summary>
    /// Classes à poser sur la racine d'un champ : <c>ui-field-filled</c> / <c>ui-field-text</c> pour l'apparence
    /// (<see cref="UiVariant.Outlined"/> est le rendu de base) et <c>ui-color-*</c> pour la couleur de mise en avant.
    /// </summary>
    public static string FieldClasses(UiVariant? variant, UiColor? color) => CssBuilder.Default()
        .AddIf(variant == UiVariant.Filled ? "ui-field-filled" : "ui-field-text", variant is UiVariant.Filled or UiVariant.Text)
        .AddIf((color ?? UiColor.Primary).ToCssClass(), color.HasValue)
        .Build();

    /// <summary>
    /// Classes d'un composant « ton » (puce, badge, alerte, encadré) : l'apparence choisie (<c>ui-v-*</c>) avec la couleur,
    /// ou, sans <paramref name="variant"/>, la classe historique <paramref name="legacyPrefix"/> + couleur.
    /// </summary>
    public static string ToneClasses(string legacyPrefix, UiColor color, UiVariant? variant) => variant is { } v
        ? $"{color.ToCssClass()} ui-v-{v.ToString().ToLowerInvariant()}"
        : $"{legacyPrefix}-{color.ToString().ToLowerInvariant()}";
}
