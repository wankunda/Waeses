namespace WaesesUI;

internal static class UiGridViewProcessor
{
    public static List<TItem> Build<TItem>(
        IEnumerable<TItem> items,
        IReadOnlyList<UiColumn<TItem>> columns,
        IReadOnlyList<UiColumn<TItem>> visibleColumns,
        bool showSearch,
        string searchQuery,
        Func<TItem, string, bool>? filterPredicate,
        IReadOnlyDictionary<string, (UiFilterOperator Operator, string? Value)> filters,
        IReadOnlyList<UiSortDefinition> sorts)
    {
        IEnumerable<TItem> result = items;

        if (showSearch && !string.IsNullOrWhiteSpace(searchQuery))
        {
            result = filterPredicate != null
                ? result.Where(item => filterPredicate(item, searchQuery))
                : result.Where(item => visibleColumns.Any(column =>
                    column.GetText(item).Contains(searchQuery, StringComparison.CurrentCultureIgnoreCase)));
        }

        foreach (var (key, (op, value)) in filters)
        {
            if (!IsActive(op, value))
            {
                continue;
            }

            var column = columns.FirstOrDefault(candidate => candidate.EffectiveKey == key);
            if (column?.Field == null)
            {
                continue;
            }

            result = result.Where(item => UiFilterEvaluator.Matches(column.GetValue(item), op, value));
        }

        IOrderedEnumerable<TItem>? ordered = null;
        foreach (var sort in sorts)
        {
            var column = columns.FirstOrDefault(candidate => candidate.EffectiveKey == sort.Key);
            if (column?.Field == null)
            {
                continue;
            }

            var selector = column.Field;
            ordered = ordered switch
            {
                null => sort.Descending ? result.OrderByDescending(selector) : result.OrderBy(selector),
                _ => sort.Descending ? ordered.ThenByDescending(selector) : ordered.ThenBy(selector)
            };
        }

        return (ordered ?? result).ToList();
    }

    private static bool IsValueless(UiFilterOperator op) => op is UiFilterOperator.IsEmpty or UiFilterOperator.IsNotEmpty;

    private static bool IsActive(UiFilterOperator op, string? value) => IsValueless(op) || !string.IsNullOrEmpty(value);
}
