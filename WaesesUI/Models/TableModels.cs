namespace WaesesUI;

/// <summary>Colonne triée (identifiée par la <c>SortKey</c> de <see cref="UiTh{TItem}"/>).</summary>
public sealed record UiSortDefinition(string Key, bool Descending);

/// <summary>Etat demandé à la source de données serveur de <see cref="UiTable{TItem}"/>.</summary>
public sealed record UiTableState(int Page, int PageSize, string? Search, IReadOnlyList<UiSortDefinition> Sorts);

/// <summary>Page de résultats renvoyée par <c>ServerData</c> : les éléments de la page et le total (toutes pages).</summary>
public sealed record UiTableData<TItem>(IReadOnlyList<TItem> Items, int TotalItems);
