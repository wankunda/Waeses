using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class TabLayoutTests : TestContext
{
    private static readonly NavigationItem[] Tabs =
    {
        new() { Text = "Accueil", Href = "", Icon = Icons.Material.Filled.Dashboard, Match = NavLinkMatch.All },
        new() { Text = "Transactions", Href = "transactions", Icon = Icons.Material.Filled.ReceiptLong },
        new() { Text = "Archives", Href = "archives", Disabled = true }
    };

    public TabLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiTabLayout> Render(UiTabBarPosition? position = null) => RenderComponent<UiTabLayout>(p =>
    {
        p.Add(c => c.BrandText, "Mon Budget")
         .Add(c => c.NavigationItems, Tabs)
         .Add(c => c.MainBody, "<p class=\"contenu\">Page</p>");
        if (position is { } value)
        {
            p.Add(c => c.TabsPosition, value);
        }
    });

    [Fact]
    public void Les_onglets_affichent_icone_et_libelle_et_marquent_l_onglet_actif()
    {
        var cut = Render();

        Assert.Contains("Mon Budget", cut.Find("header").TextContent);
        Assert.Equal("Page", cut.Find("main article .contenu").TextContent);
        Assert.Equal("Navigation principale", cut.Find("nav.ui-tablayout-tabs").GetAttribute("aria-label"));
        Assert.Equal(new[] { "Accueil", "Transactions" }, cut.FindAll("a.ui-tablayout-tab").Select(a => a.TextContent.Trim()).ToArray());
        Assert.Equal(2, cut.FindAll("a.ui-tablayout-tab .ui-icon").Count);
        Assert.Equal("Accueil", cut.Find("a.ui-tablayout-tab.active").TextContent.Trim());
    }

    [Fact]
    public void L_onglet_actif_suit_la_navigation()
    {
        var cut = Render();

        Services.GetRequiredService<FakeNavigationManager>().NavigateTo("transactions");

        cut.WaitForAssertion(() => Assert.Equal("Transactions", cut.Find("a.ui-tablayout-tab.active").TextContent.Trim()));
    }

    [Fact]
    public void Un_onglet_desactive_n_est_pas_navigable()
    {
        var disabled = Render().Find(".ui-tablayout-tab-disabled");

        Assert.Equal("SPAN", disabled.TagName);
        Assert.Equal("true", disabled.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void La_barre_est_en_bas_par_defaut_apres_le_contenu()
    {
        var root = Render().Find(".ui-tablayout");

        Assert.Contains("ui-tablayout-bottom", root.ClassList);
        Assert.Equal("NAV", root.LastElementChild!.TagName);
        Assert.Null(root.QuerySelector(".ui-tablayout-head nav"));
    }

    [Fact]
    public void En_haut_la_barre_suit_l_en_tete_avant_le_contenu()
    {
        var root = RenderComponent<UiTabLayout>(p => p
            .Add(c => c.NavigationItems, Tabs)
            .Add(c => c.TabsPosition, UiTabBarPosition.Top)
            .Add(c => c.Class, "mon-layout")).Find(".ui-tablayout");

        Assert.Contains("ui-tablayout-top", root.ClassList);
        Assert.Contains("mon-layout", root.ClassList);
        Assert.NotNull(root.QuerySelector(".ui-tablayout-head > header + nav.ui-tablayout-tabs"));
        Assert.Single(root.QuerySelectorAll("nav"));
    }

    [Fact]
    public void Le_pied_de_page_est_masque_par_defaut_et_s_affiche_sur_demande()
    {
        Assert.Empty(Render().FindAll("footer"));

        var cut = RenderComponent<UiTabLayout>(p => p.Add(c => c.ShowFooter, true).Add(c => c.FooterText, "MonBudget 1.0"));

        Assert.Equal("MonBudget 1.0", cut.Find("footer").TextContent.Trim());
    }

    [Fact]
    public void Les_trois_layouts_partagent_la_base_commune_et_gardent_leurs_valeurs_par_defaut()
    {
        Assert.True(typeof(UiLayoutBase).IsAssignableFrom(typeof(UiLayout)));
        Assert.True(typeof(UiLayoutBase).IsAssignableFrom(typeof(UiSimpleLayout)));
        Assert.True(typeof(UiLayoutBase).IsAssignableFrom(typeof(UiTabLayout)));

        Assert.True(new UiLayout().ShowFooter);
        Assert.True(new UiSimpleLayout().ShowFooter);
        Assert.False(new UiTabLayout().ShowFooter);
    }
}
