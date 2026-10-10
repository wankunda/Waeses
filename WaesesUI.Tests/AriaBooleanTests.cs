using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

/// <summary>
/// Blazor retire un attribut dont la valeur est <c>false</c> : <c>aria-checked="@bool"</c> disparaît quand la case est décochée,
/// et un rôle <c>checkbox</c> sans <c>aria-checked</c> est invalide. Ces attributs doivent toujours porter "true" ou "false".
/// </summary>
public sealed class AriaBooleanTests : TestContext
{
    public AriaBooleanTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    [Fact]
    public void Case_a_cocher_et_interrupteur_exposent_aria_checked_coche_ou_non()
    {
        var value = false;
        var box = RenderComponent<UiCheckbox>(p => p.Add(c => c.ValueExpression, () => value).Add(c => c.Label, "A"));
        var toggle = RenderComponent<UiSwitch>(p => p.Add(c => c.ValueExpression, () => value).Add(c => c.Label, "B"));

        Assert.Equal("false", box.Find("[role=checkbox]").GetAttribute("aria-checked"));
        Assert.Equal("false", toggle.Find("[role=switch]").GetAttribute("aria-checked"));

        box.Find("[role=checkbox]").Click();
        toggle.Find("[role=switch]").Click();

        Assert.Equal("true", box.Find("[role=checkbox]").GetAttribute("aria-checked"));
        Assert.Equal("true", toggle.Find("[role=switch]").GetAttribute("aria-checked"));
    }

    [Fact]
    public void Le_declencheur_du_popover_expose_aria_expanded_ferme_et_ouvert()
    {
        var cut = RenderComponent<UiPopover>(p => p.Add(c => c.Trigger, b => b.AddContent(0, "x")));

        Assert.Equal("false", cut.Find(".ui-popover-trigger").GetAttribute("aria-expanded"));
        cut.Find(".ui-popover-trigger").Click();
        Assert.Equal("true", cut.Find(".ui-popover-trigger").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Le_bouton_du_menu_de_navigation_expose_aria_expanded()
    {
        var cut = RenderComponent<UiNavMenu>(p => p.Add(c => c.NavigationItems, new[] { new NavigationItem { Text = "A", Href = "a" } }));

        var toggler = cut.Find("button.navbar-toggler");
        Assert.Contains(toggler.GetAttribute("aria-expanded"), new[] { "true", "false" });
    }

    [Fact]
    public void Le_bouton_afficher_le_mot_de_passe_expose_aria_pressed_dans_les_deux_etats()
    {
        string? value = null;
        var cut = RenderComponent<UiInput<string?>>(p => p.Add(c => c.ValueExpression, () => value).Add(c => c.Type, "password"));

        Assert.Equal("false", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));
        cut.Find("button[aria-pressed]").Click();
        Assert.Equal("true", cut.Find("button[aria-pressed]").GetAttribute("aria-pressed"));
    }

    [Theory]
    [InlineData(0, new[] { "false", "false" })]     // rien de choisi : toutes les options sont listées
    [InlineData(2, new[] { "true" })]               // « b » choisi : la liste est filtrée sur son libellé
    public void Les_options_du_combobox_exposent_aria_selected_vrai_ou_faux(int selected, string[] expected)
    {
        var current = selected;
        var cut = RenderComponent<UiCombobox<string, int>>(p => p
            .Add(c => c.Items, new[] { "a", "b" })
            .Add(c => c.DisplaySelector, s => s)
            .Add(c => c.ValueSelector, s => s == "a" ? 1 : 2)
            .Add(c => c.Value, current)
            .Add(c => c.ValueExpression, () => current));

        cut.Find("input").Focus();

        Assert.Equal(expected, cut.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")).ToArray());
    }

    [Fact]
    public void Le_bloc_de_code_est_atteignable_au_clavier_et_nomme()
    {
        var cut = RenderComponent<UiCodeBlock>(p => p.Add(c => c.Code, "x = 1").Add(c => c.Language, "csharp"));

        var pre = cut.Find("pre");
        Assert.Equal("0", pre.GetAttribute("tabindex"));
        Assert.Equal("region", pre.GetAttribute("role"));
        Assert.Equal("Code csharp", pre.GetAttribute("aria-label"));
    }
}
