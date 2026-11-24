namespace WaesesUI;

/// <summary>Un nœud typé d'une arborescence <see cref="UiTreeView{TItem}"/>.</summary>
public sealed class UiTreeNode<TItem>
{
    public UiTreeNode(
        TItem item,
        string label,
        IEnumerable<UiTreeNode<TItem>>? children = null,
        bool isDisabled = false,
        bool initiallyExpanded = false)
    {
        ArgumentNullException.ThrowIfNull(label);

        Item = item;
        Label = label;
        Children = children?.ToArray() ?? Array.Empty<UiTreeNode<TItem>>();
        IsDisabled = isDisabled;
        InitiallyExpanded = initiallyExpanded;
    }

    /// <summary>La donnée associée à ce nœud.</summary>
    public TItem Item { get; init; }

    /// <summary>Le libellé accessible affiché par défaut.</summary>
    public string Label { get; init; }

    /// <summary>Les nœuds enfants.</summary>
    public IReadOnlyList<UiTreeNode<TItem>> Children { get; init; }

    /// <summary>Indique si ce nœud ne peut être ni sélectionné ni développé.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Indique si ce nœud est développé au premier rendu.</summary>
    public bool InitiallyExpanded { get; init; }
}
