using System.Globalization;

namespace WaesesUI;

public enum UiFilterOperator
{
    Contains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    IsEmpty,
    IsNotEmpty
}

public enum UiGridEditMode
{
    None,

    /// <summary>Une ligne à la fois devient modifiable (boutons Modifier / Enregistrer / Annuler).</summary>
    Row
}

/// <summary>Filtre d'une colonne (identifiée par sa <c>Key</c>).</summary>
public sealed record UiFilterDefinition(string Key, UiFilterOperator Operator, string? Value);

/// <summary>État demandé à la source de données serveur de <see cref="UiDataGrid{TItem}"/>.</summary>
public sealed record UiGridState(int Page, int PageSize, string? Search, IReadOnlyList<UiSortDefinition> Sorts, IReadOnlyList<UiFilterDefinition> Filters);

/// <summary>Page de résultats renvoyée par <c>ServerData</c> : les éléments de la page et le total (toutes pages).</summary>
public sealed record UiGridData<TItem>(IReadOnlyList<TItem> Items, int TotalItems);

/// <summary>Modification validée d'une ligne : l'élément d'origine et sa copie modifiée.</summary>
public sealed record UiRowEdit<TItem>(TItem Original, TItem Edited);

/// <summary>Groupe de lignes affiché sous un en-tête quand <c>GroupBy</c> est défini.</summary>
public sealed record UiGridGroup<TItem>(string Key, string Label, IReadOnlyList<TItem> Items);

/// <summary>Évalue un filtre de colonne sur une valeur : comparaison typée quand l'opérande se convertit, texte insensible à la casse sinon.</summary>
public static class UiFilterEvaluator
{
    public static bool Matches(object? value, UiFilterOperator op, string? operand, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        var text = ToText(value, culture);

        switch (op)
        {
            case UiFilterOperator.IsEmpty: return string.IsNullOrEmpty(text);
            case UiFilterOperator.IsNotEmpty: return !string.IsNullOrEmpty(text);
        }

        if (string.IsNullOrEmpty(operand)) return true;   // filtre sans valeur : n'exclut rien

        switch (op)
        {
            case UiFilterOperator.Contains: return text.Contains(operand, StringComparison.CurrentCultureIgnoreCase);
            case UiFilterOperator.StartsWith: return text.StartsWith(operand, StringComparison.CurrentCultureIgnoreCase);
            case UiFilterOperator.EndsWith: return text.EndsWith(operand, StringComparison.CurrentCultureIgnoreCase);
        }

        var comparison = Compare(value, text, operand, culture);
        return op switch
        {
            UiFilterOperator.Equals => comparison == 0,
            UiFilterOperator.NotEquals => comparison != 0,
            UiFilterOperator.GreaterThan => comparison > 0,
            UiFilterOperator.GreaterThanOrEqual => comparison >= 0,
            UiFilterOperator.LessThan => comparison < 0,
            UiFilterOperator.LessThanOrEqual => comparison <= 0,
            _ => true
        };
    }

    private static string ToText(object? value, CultureInfo culture) =>
        value is IFormattable formattable ? formattable.ToString(null, culture) : value?.ToString() ?? string.Empty;

    private static int Compare(object? value, string text, string operand, CultureInfo culture)
    {
        if (value is not null && TryConvert(operand, value.GetType(), culture, out var typed) && value is IComparable comparable)
        {
            return comparable.CompareTo(typed);
        }
        return string.Compare(text, operand, StringComparison.CurrentCultureIgnoreCase);
    }

    private static bool TryConvert(string operand, Type type, CultureInfo culture, out object? result)
    {
        result = null;
        type = Nullable.GetUnderlyingType(type) ?? type;
        try
        {
            if (type == typeof(string)) return false;                       // comparaison texte
            if (type == typeof(DateTime)) { if (DateTime.TryParse(operand, culture, DateTimeStyles.None, out var d)) { result = d; return true; } return false; }
            if (type == typeof(DateOnly)) { if (DateOnly.TryParse(operand, culture, DateTimeStyles.None, out var d)) { result = d; return true; } return false; }
            if (type == typeof(TimeSpan)) { if (TimeSpan.TryParse(operand, culture, out var t)) { result = t; return true; } return false; }
            if (type == typeof(bool)) { if (bool.TryParse(operand, out var b)) { result = b; return true; } return false; }
            if (type.IsEnum) { if (Enum.TryParse(type, operand, true, out var e)) { result = e; return true; } return false; }
            if (typeof(IConvertible).IsAssignableFrom(type))
            {
                result = Convert.ChangeType(operand, type, culture);
                return true;
            }
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidCastException)
        {
            return false;
        }
        return false;
    }
}
