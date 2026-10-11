using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class NavBarTests : TestContext
{
    private static readonly NavigationItem[] Items =
    {
        new() { Text = "Accueil", Href = "", Match = NavLinkMatch.All },
        new() { Text = "Activités", Href = "activites", Icon = Icons.Material.Filled.Home },
        new() { Text = "Archives", Href = "archives", Disabled = true }
    };

    public NavBarTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiNavBar> Render() => RenderComponent<UiNavBar>(p => p
        .Add(c => c.BrandText, "FASS KOR")
        .Add(c => c.NavigationItems, Items));

    [Fact]
    public void La_barre_affiche_le_nom_les_liens_et_marque_le_lien_actif()
    {
        var cut = Render();

        Assert.Equal("FASS KOR", cut.Find("a.ui-navbar-brand").TextContent.Trim());
        Assert.Equal(new[] { "Accueil", "Activités" }, cut.FindAll("a.ui-navbar-link").Select(a => a.TextContent.Trim()).ToArray());
        Assert.Equal("Accueil", cut.Find("a.ui-navbar-link.active").TextContent.Trim());
        Assert.Single(cut.FindAll("a.ui-navbar-link .ui-icon"));
        Assert.Equal("Navigation principale", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void Un_lien_desactive_n_est_pas_navigable()
    {
        var disabled = Render().Find(".ui-navbar-link-disabled");

        Assert.Equal("SPAN", disabled.TagName);
        Assert.Equal("true", disabled.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Le_bouton_hamburger_ouvre_et_ferme_le_menu_qu_il_controle()
    {
        var cut = Render();
        var menu = cut.Find(".ui-navbar-menu");

        Assert.Equal("false", cut.Find(".ui-navbar-toggle").GetAttribute("aria-expanded"));
        Assert.Equal(menu.Id, cut.Find(".ui-navbar-toggle").GetAttribute("aria-controls"));
        Assert.DoesNotContain("ui-navbar-menu-open", menu.ClassList);

        cut.Find(".ui-navbar-toggle").Click();

        Assert.Equal("true", cut.Find(".ui-navbar-toggle").GetAttribute("aria-expanded"));
        Assert.Equal("Fermer le menu de navigation", cut.Find(".ui-navbar-toggle").GetAttribute("aria-label"));
        Assert.Contains("ui-navbar-menu-open", cut.Find(".ui-navbar-menu").ClassList);

        cut.Find(".ui-navbar-toggle").Click();

        Assert.Equal("false", cut.Find(".ui-navbar-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Le_menu_ouvert_se_ferme_au_choix_d_un_lien_avec_Echap_et_a_la_navigation()
    {
        var cut = Render();
        void Open() => cut.Find(".ui-navbar-toggle").Click();
        bool IsOpen() => cut.Find(".ui-navbar-menu").ClassList.Contains("ui-navbar-menu-open");

        Open();
        cut.Find("a.ui-navbar-link").Click();
        Assert.False(IsOpen());

        Open();
        cut.Find("header").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.False(IsOpen());

        Open();
        Services.GetRequiredService<FakeNavigationManager>().NavigateTo("activites");
        cut.WaitForAssertion(() => Assert.False(IsOpen()));
    }

    [Theory]
    [InlineData(UiBreakpoint.Sm, "ui-navbar-collapse-sm")]
    [InlineData(UiBreakpoint.Md, "ui-navbar-collapse-md")]
    [InlineData(UiBreakpoint.Lg, "ui-navbar-collapse-lg")]
    public void Le_point_de_repli_et_les_options_se_traduisent_en_classes(UiBreakpoint breakpoint, string expected)
    {
        var cut = RenderComponent<UiNavBar>(p => p
            .Add(c => c.CollapseBelow, breakpoint)
            .Add(c => c.Sticky, false)
            .Add(c => c.Class, "ma-barre")
            .Add(c => c.BrandContent, "<img alt=\"Logo\" />")
            .Add(c => c.EndContent, "<button>Connexion</button>"));

        var header = cut.Find("header");
        Assert.Contains(expected, header.ClassList);
        Assert.Contains("ma-barre", header.ClassList);
        Assert.DoesNotContain("ui-navbar-sticky", header.ClassList);
        Assert.NotNull(cut.Find(".ui-navbar-brand img"));
        Assert.Equal("Connexion", cut.Find(".ui-navbar-end button").TextContent);
    }
}
