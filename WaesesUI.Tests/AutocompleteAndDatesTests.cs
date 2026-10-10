using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class AutocompleteAndDatesTests : TestContext
{
    private sealed record City(int Id, string Name);

    private static readonly City[] Cities = { new(1, "Paris"), new(2, "Lyon"), new(3, "Nice") };

    public AutocompleteAndDatesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiAutocomplete<City, int>> RenderAutocomplete(Action<int>? onChange = null, Action<ComponentParameterCollectionBuilder<UiAutocomplete<City, int>>>? extra = null)
    {
        var current = 0;
        return RenderComponent<UiAutocomplete<City, int>>(p =>
        {
            p.Add(c => c.Items, Cities);
            p.Add(c => c.DisplaySelector, c => c.Name);
            p.Add(c => c.ValueSelector, c => c.Id);
            p.Add(c => c.Value, 0);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<int>(this, v => { current = v; onChange?.Invoke(v); }));
            p.Add(c => c.ValueExpression, () => current);
            p.Add(c => c.Label, "Ville");
            p.Add(c => c.DebounceDelay, 0);
            extra?.Invoke(p);
        });
    }

    // ---------- Autocomplete ----------
    [Fact]
    public void Autocomplete_sans_provider_garde_la_liste_en_place_et_le_JS_de_clic_exterieur()
    {
        var cut = RenderAutocomplete();
        cut.Find("input").Focus();

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".ui-autocomplete-option").Count));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.setupAutocomplete");
        Assert.Equal("listbox", cut.Find(".ui-autocomplete-dropdown").GetAttribute("role"));
    }

    [Fact]
    public void Autocomplete_avec_provider_rend_la_liste_dans_le_provider_sans_JS_de_clic_exterieur()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderAutocomplete();
        cut.Find("input").Focus();

        provider.WaitForAssertion(() => Assert.Equal(3, provider.FindAll(".ui-popover-layer .ui-autocomplete-option").Count));
        Assert.Empty(cut.FindAll(".ui-autocomplete-option"));
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "UiInterop.setupAutocomplete");
        Assert.Equal(provider.Find("[role=listbox]").Id, cut.Find("input").GetAttribute("aria-controls"));
    }

    [Fact]
    public void Autocomplete_choisir_une_option_du_provider_met_a_jour_la_valeur_et_ferme()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var chosen = 0;
        var cut = RenderAutocomplete(v => chosen = v);
        cut.Find("input").Focus();
        provider.WaitForAssertion(() => provider.FindAll(".ui-autocomplete-option"));

        provider.FindAll(".ui-autocomplete-option")[2].Click();

        Assert.Equal(3, chosen);
        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Equal("Nice", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public async Task Autocomplete_se_ferme_via_le_provider_et_restaure_le_libelle()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var cut = RenderAutocomplete();
        cut.Find("input").Focus();
        provider.WaitForAssertion(() => provider.Find(".ui-popover-layer"));
        provider.FindAll(".ui-autocomplete-option")[1].Click();
        cut.Find("input").Focus();
        cut.Find("input").Input("zzz");
        provider.WaitForAssertion(() => provider.Find(".ui-autocomplete-empty"));

        var entry = Services.GetRequiredService<IUiPopoverService>().Entries.Single(e => e.Open);
        await cut.InvokeAsync(() => entry.RequestClose!());

        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Equal("Lyon", cut.Find("input").GetAttribute("value"));
    }

    [Fact]
    public void Autocomplete_expose_aria_invalid_et_aria_describedby()
    {
        var cut = RenderAutocomplete(extra: p => p.Add(c => c.ErrorMessage, "Ville requise"));
        var input = cut.Find("input");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, input.GetAttribute("aria-describedby"));
    }

    [Fact]
    public async Task Autocomplete_participe_a_la_validation_du_formulaire()
    {
        var model = new CityModel();
        var context = new EditContext(model);
        var cut = RenderComponent<UiAutocomplete<City, int>>(p => p
            .Add(c => c.Items, Cities)
            .Add(c => c.DisplaySelector, c => c.Name)
            .Add(c => c.ValueSelector, c => c.Id)
            .Add(c => c.Value, model.CityId)
            .Add(c => c.ValueExpression, () => model.CityId)
            .Add(c => c.Validation, v => v == 0 ? "Ville requise" : null)
            .AddCascadingValue(context));

        Assert.False(await cut.InvokeAsync(() => context.Validate()));
        cut.WaitForAssertion(() => Assert.Contains("Ville requise", cut.Find("[role=alert]").TextContent));
    }

    private sealed class CityModel
    {
        public int CityId { get; set; }
        public DateTime? Date { get; set; }
    }

    // ---------- DatePicker / DateTimePicker ----------
    [Fact]
    public async Task DatePicker_utilise_la_base_commune_validation_aria_et_id()
    {
        var model = new CityModel();
        var context = new EditContext(model);
        var cut = RenderComponent<UiDatePicker>(p => p
            .Add(c => c.Value, model.Date)
            .Add(c => c.ValueExpression, () => model.Date)
            .Add(c => c.Validation, d => d is null ? "Date requise" : null)
            .AddCascadingValue(context));

        Assert.StartsWith("datepicker-", cut.Find("button.ui-datepicker-trigger").Id);

        Assert.False(await cut.InvokeAsync(() => context.Validate()));
        cut.WaitForAssertion(() => Assert.Contains("Date requise", cut.Find("[role=alert]").TextContent));
        var trigger = cut.Find("button.ui-datepicker-trigger");
        Assert.Equal("true", trigger.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, trigger.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void DateTimePicker_affiche_l_erreur_via_la_base_commune()
    {
        DateTime? value = null;
        var cut = RenderComponent<UiDateTimePicker>(p => p
            .Add(c => c.ValueExpression, () => value)
            .Add(c => c.ErrorMessage, "Date invalide"));

        var group = cut.Find("[role=group]");
        Assert.Equal("true", group.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, group.GetAttribute("aria-describedby"));
        Assert.StartsWith("datetime-", group.Id);
    }

    // ---------- DateRange / TagInput ----------
    [Fact]
    public void DateRange_signale_une_plage_inversee_via_aria()
    {
        var cut = RenderComponent<UiDateRangePicker>(p => p
            .Add(c => c.Id, "range")
            .Add(c => c.Start, new DateTime(2026, 5, 10))
            .Add(c => c.End, new DateTime(2026, 5, 1)));

        var start = cut.Find("#range");
        Assert.Equal("true", start.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, start.GetAttribute("aria-describedby"));
    }

    [Fact]
    public void TagInput_relie_son_message_d_erreur_au_champ()
    {
        var cut = RenderComponent<UiTagInput>(p => p.Add(c => c.Id, "tags").Add(c => c.ErrorMessage, "Au moins une étiquette"));

        var input = cut.Find("#tags");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find("[role=alert]").Id, input.GetAttribute("aria-describedby"));
    }
}
