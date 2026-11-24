using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace WaesesUI.Tests;

public sealed class TreeViewTests : TestContext
{
    [Fact]
    public void Rend_une_arborescence_avec_roles_niveaux_et_gabarit()
    {
        var child = new UiTreeNode<string>("child", "Enfant");
        var root = new UiTreeNode<string>("root", "Racine", [child], initiallyExpanded: true);

        var cut = RenderComponent<UiTreeView<string>>(parameters => parameters
            .Add(component => component.Nodes, [root])
            .Add(component => component.NodeTemplate, node => builder => builder.AddContent(0, $"Nœud : {node.Label}")));

        Assert.Equal("tree", cut.Find("[role=tree]").GetAttribute("role"));
        Assert.Equal("Arborescence", cut.Find("[role=tree]").GetAttribute("aria-label"));
        Assert.Equal(new[] { "⌄Nœud : Racine", "Nœud : Enfant" }, cut.FindAll(".ui-treeview-row").Select(item => item.TextContent.Trim()).ToArray());
        Assert.Equal(new[] { "1", "2" }, cut.FindAll("[role=treeitem]").Select(item => item.GetAttribute("aria-level")).ToArray());
        Assert.Equal("true", cut.FindAll("[role=treeitem]")[0].GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Selectionner_un_nœud_notifie_le_changement_et_met_a_jour_aria_selected()
    {
        var node = new UiTreeNode<string>("one", "Premier");
        UiTreeNode<string>? selected = null;
        var cut = RenderComponent<UiTreeView<string>>(parameters => parameters
            .Add(component => component.Nodes, [node])
            .Add(component => component.SelectedNodeChanged,
                EventCallback.Factory.Create<UiTreeNode<string>?>(this, value => selected = value)));

        cut.Find("[role=treeitem]").Click();

        Assert.Same(node, selected);
        Assert.Equal("true", cut.Find("[role=treeitem]").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Les_boutons_de_developpement_et_reduction_notifient_les_transitions()
    {
        var root = new UiTreeNode<string>("root", "Racine", [new UiTreeNode<string>("child", "Enfant")]);
        var expanded = new List<UiTreeNode<string>>();
        var collapsed = new List<UiTreeNode<string>>();
        var cut = RenderComponent<UiTreeView<string>>(parameters => parameters
            .Add(component => component.Nodes, [root])
            .Add(component => component.NodeExpanded,
                EventCallback.Factory.Create<UiTreeNode<string>>(this, expanded.Add))
            .Add(component => component.NodeCollapsed,
                EventCallback.Factory.Create<UiTreeNode<string>>(this, collapsed.Add)));

        Assert.Single(cut.FindAll("[role=treeitem]"));
        cut.Find("button.ui-treeview-toggle").Click();

        Assert.Equal(new[] { "Racine", "Enfant" }, cut.FindAll(".ui-treeview-row")
            .Select(item => item.TextContent.Trim().TrimStart('⌄', '›')).ToArray());
        Assert.Equal(new[] { root }, expanded);

        cut.Find("button.ui-treeview-toggle").Click();

        Assert.Single(cut.FindAll("[role=treeitem]"));
        Assert.Equal(new[] { root }, collapsed);
    }

    [Fact]
    public void Un_nœud_desactive_ne_peut_pas_etre_selectionne_ni_developpe()
    {
        var disabled = new UiTreeNode<string>("disabled", "Désactivé", [new UiTreeNode<string>("child", "Enfant")], isDisabled: true);
        UiTreeNode<string>? selected = null;
        var expanded = new List<UiTreeNode<string>>();
        var cut = RenderComponent<UiTreeView<string>>(parameters => parameters
            .Add(component => component.Nodes, [disabled])
            .Add(component => component.SelectedNodeChanged,
                EventCallback.Factory.Create<UiTreeNode<string>?>(this, value => selected = value))
            .Add(component => component.NodeExpanded,
                EventCallback.Factory.Create<UiTreeNode<string>>(this, expanded.Add)));

        var treeItem = cut.Find("[role=treeitem]");
        Assert.Equal("true", treeItem.GetAttribute("aria-disabled"));
        Assert.True(cut.Find("button.ui-treeview-toggle").HasAttribute("disabled"));

        treeItem.Click();
        cut.Find("[role=tree]").KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Null(selected);
        Assert.Empty(expanded);
        Assert.Single(cut.FindAll("[role=treeitem]"));
    }

    [Fact]
    public void Navigation_au_clavier_ignore_les_nœuds_desactives_et_permet_selection_et_developpement()
    {
        var disabled = new UiTreeNode<string>("disabled", "Désactivé", isDisabled: true);
        var child = new UiTreeNode<string>("child", "Enfant");
        var root = new UiTreeNode<string>("root", "Racine", [child]);
        UiTreeNode<string>? selected = null;
        var expanded = new List<UiTreeNode<string>>();
        var cut = RenderComponent<UiTreeView<string>>(parameters => parameters
            .Add(component => component.Nodes, [disabled, root])
            .Add(component => component.SelectedNodeChanged,
                EventCallback.Factory.Create<UiTreeNode<string>?>(this, value => selected = value))
            .Add(component => component.NodeExpanded,
                EventCallback.Factory.Create<UiTreeNode<string>>(this, expanded.Add)));

        var tree = cut.Find("[role=tree]");
        tree.KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal(cut.FindAll("[role=treeitem]")[1].Id, tree.GetAttribute("aria-activedescendant"));

        tree.KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(new[] { root }, expanded);
        Assert.Equal(3, cut.FindAll("[role=treeitem]").Count);

        tree.KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });
        Assert.Equal(cut.FindAll("[role=treeitem]")[2].Id, tree.GetAttribute("aria-activedescendant"));
        tree.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Same(child, selected);
        Assert.Equal("true", cut.FindAll("[role=treeitem]")[2].GetAttribute("aria-selected"));
    }
}
