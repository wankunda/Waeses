using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class DropdownProviderTests : TestContext
{
    private sealed record City(int Id, string Name);

    private static readonly City[] Cities = { new(1, "Paris"), new(2, "Lyon"), new(3, "Nice") };

    public DropdownProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiCombobox<City, int>> RenderCombobox(int value = 0, Action<int>? onChange = null)
    {
        var current = value;
        return RenderComponent<UiCombobox<City, int>>(p => p
            .Add(c => c.Items, Cities)
            .Add(c => c.DisplaySelector, c => c.Name)
            .Add(c => c.ValueSelector, c => c.Id)
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => { current = v; onChange?.Invoke(v); }))
            .Add(c => c.ValueExpression, () => current)
            .Add(c => c.Label, "Ville"));
    }

    // ---------- Sans provider : rendu en place (compatibilité) ----------
    [Fact]
    public void Combobox_sans_provider_rend_la_liste_en_place()
    {
        var cut = RenderCombobox();
        cut.Find("input").Focus();

        Assert.Equal(3, cut.FindAll(".ui-combobox .ui-combobox-option").Count);
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-expanded"));
    }

    // ---------- Avec provider ----------
    [Fact]
    public void Combobox_avec_provider_rend_la_liste_dans_le_provider()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderCombobox();

        Assert.Empty(provider.FindAll(".ui-combobox-option"));

        cut.Find("input").Focus();

        provider.WaitForAssertion(() => Assert.Equal(3, provider.FindAll(".ui-popover-layer .ui-combobox-option").Count));
        Assert.Empty(cut.FindAll(".ui-combobox-option"));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.openPopover");
    }

    [Fact]
    public void Combobox_choisir_une_option_depuis_le_provider_met_a_jour_la_valeur_et_ferme()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var chosen = 0;
        var cut = RenderCombobox(onChange: v => chosen = v);
        cut.Find("input").Focus();
        provider.WaitForAssertion(() => provider.FindAll(".ui-combobox-option"));

        provider.FindAll(".ui-combobox-option")[1].MouseDown();

        Assert.Equal(2, chosen);
        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Equal("Lyon", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Combobox_se_ferme_au_clic_exterieur_et_restaure_le_libelle_selectionne()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderCombobox(value: 3);
        cut.Find("input").Focus();
        cut.Find("input").Input("Pa");
        provider.WaitForAssertion(() => provider.Find(".ui-popover-layer"));

        var entry = Services.GetRequiredService<IUiPopoverService>().Entries.Single(e => e.Open);
        await cut.InvokeAsync(() => entry.RequestClose!());   // ce que fait le JS sur un clic exterieur

        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Equal("Nice", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Combobox_expose_les_attributs_aria_du_champ()
    {
        var cut = RenderCombobox();
        cut.SetParametersAndRender(p => p.Add(c => c.ErrorMessage, "Choix requis"));

        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, input.GetAttribute("aria-describedby"));
    }

    // ---------- MultiSelect ----------
    private IRenderedComponent<UiMultiSelect<City, int>> RenderMulti(Action<IReadOnlyList<int>>? onChange = null) =>
        RenderComponent<UiMultiSelect<City, int>>(p => p
            .Add(c => c.Items, Cities)
            .Add(c => c.DisplaySelector, c => c.Name)
            .Add(c => c.ValueSelector, c => c.Id)
            .Add(c => c.Label, "Villes")
            .Add(c => c.SelectedValuesChanged, EventCallback.Factory.Create<IReadOnlyList<int>>(this, v => onChange?.Invoke(v))));

    [Fact]
    public void MultiSelect_avec_provider_ouvre_son_menu_dans_le_provider_ancre_sur_le_declencheur()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderMulti();

        cut.Find("button.ui-multiselect-toggle").Click();

        provider.WaitForAssertion(() => Assert.Equal(3, provider.FindAll(".ui-popover-layer [role=option]").Count));
        Assert.Empty(cut.FindAll("[role=option]"));
        Assert.Equal("true", cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-expanded"));
        Assert.Equal(provider.Find("[role=listbox]").Id, cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-controls"));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.openPopover"
            && i.Arguments.Count > 2 && i.Arguments[2]!.ToString()!.Contains("anchorSelector = [id='"));
    }

    [Fact]
    public void MultiSelect_cocher_une_option_du_provider_notifie_la_selection()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        IReadOnlyList<int>? selected = null;
        var cut = RenderMulti(v => selected = v);
        cut.Find("button.ui-multiselect-toggle").Click();
        provider.WaitForAssertion(() => provider.FindAll("[role=option]"));

        provider.FindAll("[role=option]")[2].Click();

        Assert.Equal(new[] { 3 }, selected);
    }

    [Fact]
    public async Task MultiSelect_se_ferme_via_la_demande_du_provider()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderMulti();
        cut.Find("button.ui-multiselect-toggle").Click();
        provider.WaitForAssertion(() => provider.Find(".ui-popover-layer"));

        var entry = Services.GetRequiredService<IUiPopoverService>().Entries.Single(e => e.Open);
        await cut.InvokeAsync(() => entry.RequestClose!());

        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Equal("false", cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void MultiSelect_sans_provider_garde_le_menu_en_place()
    {
        var cut = RenderMulti();
        cut.Find("button.ui-multiselect-toggle").Click();
        Assert.Equal(3, cut.FindAll(".ui-multiselect-menu [role=option]").Count);
    }
}
