using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

/// <summary>Groupes de <see cref="UiNavMenu"/> : titre puis liens en retrait, groupes fixes ou repliables.</summary>
public sealed class NavMenuGroupsTests : TestContext
{
    private static readonly NavigationItem[] Items =
    {
        new() { Text = "Tableau de bord", Href = "accueil" },
        new() { Text = "Clients", Href = "clients", Group = "Général" },
        new() { Text = "Contacts", Href = "contacts", Group = "Général" },
        new() { Text = "Factures", Href = "factures", Group = "Gestion" },
        new() { Text = "Archives", Href = "archives", Group = "Gestion" }
    };

    public NavMenuGroupsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    [Fact]
    public void Les_liens_d_un_groupe_sont_sous_son_titre_et_les_liens_sans_groupe_restent_simples()
    {
        var cut = RenderComponent<UiNavMenu>(p => p.Add(c => c.NavigationItems, Items));

        var groups = cut.FindAll(".nav-group");
        Assert.Equal(2, groups.Count);
        Assert.Equal("Général", groups[0].QuerySelector(".nav-group-title")!.TextContent.Trim());
        Assert.Equal(2, groups[0].QuerySelectorAll(".nav-group-items .nav-link").Length);

        // Le lien sans groupe est un enfant direct de la navigation, hors de tout groupe.
        Assert.Single(cut.FindAll("nav > .nav-item"));

        // Par défaut, les groupes sont fixes : pas de bouton.
        Assert.Empty(cut.FindAll(".nav-group-toggle"));
        var items = groups[0].QuerySelector(".nav-group-items")!;
        Assert.Equal("group", items.GetAttribute("role"));
        Assert.Equal(groups[0].QuerySelector(".nav-group-title")!.Id, items.GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void CollapsibleGroups_rend_tous_les_groupes_repliables()
    {
        var cut = RenderComponent<UiNavMenu>(p => p.Add(c => c.NavigationItems, Items).Add(c => c.CollapsibleGroups, true));

        var toggle = cut.FindAll(".nav-group-toggle")[1];
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        var itemsId = toggle.GetAttribute("aria-controls");

        toggle.Click();

        toggle = cut.FindAll(".nav-group-toggle")[1];
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.True(cut.Find($"#{itemsId}").HasAttribute("hidden"));
        Assert.Contains("nav-group-collapsed", cut.FindAll(".nav-group")[1].ClassList);

        toggle.Click();
        Assert.False(cut.Find($"#{itemsId}").HasAttribute("hidden"));
    }

    [Fact]
    public void Groups_permet_de_meler_groupes_fixes_et_repliables()
    {
        var cut = RenderComponent<UiNavMenu>(p => p
            .Add(c => c.NavigationItems, Items)
            .Add(c => c.Groups, new[] { new NavigationGroup("Gestion") { Collapsible = true, InitiallyCollapsed = true } }));

        var groups = cut.FindAll(".nav-group");
        Assert.Null(groups[0].QuerySelector(".nav-group-toggle"));             // Général : fixe
        var toggle = groups[1].QuerySelector(".nav-group-toggle")!;            // Gestion : repliable, replié
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Un_groupe_peut_rester_fixe_quand_les_autres_sont_repliables()
    {
        var cut = RenderComponent<UiNavMenu>(p => p
            .Add(c => c.NavigationItems, Items)
            .Add(c => c.CollapsibleGroups, true)
            .Add(c => c.Groups, new[] { new NavigationGroup("Général") { Collapsible = false } }));

        var groups = cut.FindAll(".nav-group");
        Assert.Null(groups[0].QuerySelector(".nav-group-toggle"));
        Assert.NotNull(groups[1].QuerySelector(".nav-group-toggle"));
    }

    [Fact]
    public void Le_groupe_de_la_page_active_reste_ouvert()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("factures/2024");

        var cut = RenderComponent<UiNavMenu>(p => p
            .Add(c => c.NavigationItems, Items)
            .Add(c => c.Groups, new[]
            {
                new NavigationGroup("Général") { Collapsible = true, InitiallyCollapsed = true },
                new NavigationGroup("Gestion") { Collapsible = true, InitiallyCollapsed = true }
            }));

        var toggles = cut.FindAll(".nav-group-toggle");
        Assert.Equal("false", toggles[0].GetAttribute("aria-expanded"));
        Assert.Equal("true", toggles[1].GetAttribute("aria-expanded"));     // contient la page "factures"

        // En allant sur une page de "Général", ce groupe s'ouvre à son tour.
        navigation.NavigateTo("contacts");
        cut.WaitForAssertion(() => Assert.Equal("true", cut.FindAll(".nav-group-toggle")[0].GetAttribute("aria-expanded")));
    }

    [Fact]
    public void Cliquer_sur_un_titre_de_groupe_ne_referme_pas_le_tiroir()
    {
        var cut = RenderComponent<UiNavMenu>(p => p
            .Add(c => c.NavigationItems, Items)
            .Add(c => c.CollapsibleGroups, true)
            .Add(c => c.IsOpen, true));

        cut.FindAll(".nav-group-toggle")[0].Click();

        Assert.Contains("nav-drawer-open", cut.Find(".nav-drawer").ClassList);
    }
}
