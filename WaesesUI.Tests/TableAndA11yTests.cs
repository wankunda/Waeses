using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class TableAndA11yTests : TestContext
{
    private sealed record Person(string Name, int Age);

    private static readonly Person[] People =
    {
        new("Cora", 30), new("Abel", 40), new("Bruno", 30), new("Dina", 20),
    };

    private static readonly Func<Person, object> ByName = p => p.Name;
    private static readonly Func<Person, object> ByAge = p => p.Age;

    public TableAndA11yTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private static RenderFragment Header(bool withKeys = true) => b =>
    {
        var seq = 0;
        foreach (var (title, sel, key) in new[] { ("Nom", ByName, "name"), ("Age", ByAge, "age") })
        {
            b.OpenComponent<UiTh<Person>>(seq++);
            b.AddAttribute(seq++, "Title", title);
            b.AddAttribute(seq++, "SortBy", sel);
            if (withKeys) b.AddAttribute(seq++, "SortKey", key);
            b.CloseComponent();
        }
    };

    private static RenderFragment<Person> Row => p => b =>
    {
        b.OpenElement(0, "td");
        b.AddContent(1, p.Name);
        b.CloseElement();
        b.OpenElement(2, "td");
        b.AddContent(3, p.Age);
        b.CloseElement();
    };

    private IRenderedComponent<UiTable<Person>> RenderTable(Action<ComponentParameterCollectionBuilder<UiTable<Person>>>? extra = null, bool paginated = false) =>
        RenderComponent<UiTable<Person>>(p =>
        {
            p.Add(c => c.Items, People);
            p.Add(c => c.HeaderContent, Header());
            p.Add(c => c.RowTemplate, Row);
            p.Add(c => c.Paginated, paginated);
            extra?.Invoke(p);
        });

    private static string[] Names(IRenderedComponent<UiTable<Person>> cut) =>
        cut.FindAll("tbody tr:not(.ui-table-detail-row) td:first-of-type").Select(td => td.TextContent.Trim()).ToArray();

    // ---------- Table : tri ----------
    [Fact]
    public void Tri_croissant_puis_decroissant_avec_aria_sort()
    {
        var cut = RenderTable();
        var nameHeader = () => cut.FindAll("th")[0];
        Assert.Equal("none", nameHeader().GetAttribute("aria-sort"));

        nameHeader().Click();
        Assert.Equal(new[] { "Abel", "Bruno", "Cora", "Dina" }, Names(cut));
        Assert.Equal("ascending", nameHeader().GetAttribute("aria-sort"));

        nameHeader().Click();
        Assert.Equal(new[] { "Dina", "Cora", "Bruno", "Abel" }, Names(cut));
        Assert.Equal("descending", nameHeader().GetAttribute("aria-sort"));
    }

    [Fact]
    public void Tri_multi_colonnes_avec_Maj_clic()
    {
        var cut = RenderTable(p => p.Add(c => c.MultiSort, true));
        cut.FindAll("th")[1].Click();                                                  // age asc
        cut.FindAll("th")[0].Click(new MouseEventArgs { ShiftKey = true });            // puis nom asc

        Assert.Equal(new[] { "Dina", "Bruno", "Cora", "Abel" }, Names(cut));
        Assert.Contains("<sup>2</sup>", cut.FindAll("th")[0].InnerHtml);
    }

    [Fact]
    public void Sans_MultiSort_Maj_clic_remplace_le_tri()
    {
        var cut = RenderTable();
        cut.FindAll("th")[1].Click();
        cut.FindAll("th")[0].Click(new MouseEventArgs { ShiftKey = true });

        Assert.Equal(new[] { "Abel", "Bruno", "Cora", "Dina" }, Names(cut));
    }

    [Fact]
    public void Tri_au_clavier_avec_Entree()
    {
        var cut = RenderTable();
        cut.FindAll("th")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal("Abel", Names(cut)[0]);
    }

    [Fact]
    public void Recherche_filtre_les_lignes_et_la_page_courante_est_bornee()
    {
        var cut = RenderTable(p => p
            .Add(c => c.ShowSearch, true)
            .Add(c => c.PageSize, 2)
            .Add(c => c.CurrentPage, 9), paginated: true);

        // Page demandée hors limites : affichée bornée à la dernière.
        Assert.Equal(new[] { "Bruno", "Dina" }, Names(cut));

        cut.Find("input[type=search]").Input("cor");
        Assert.Equal(new[] { "Cora" }, Names(cut));
    }

    // ---------- Table : lignes extensibles, sélection ----------
    [Fact]
    public void Ligne_extensible_expose_aria_expanded_et_affiche_le_detail()
    {
        var cut = RenderTable(p => p.Add(c => c.ChildRowContent, (Person x) => (RenderFragment)(b => b.AddContent(0, $"detail-{x.Name}"))));
        Assert.Empty(cut.FindAll(".ui-table-detail-row"));

        var button = cut.FindAll(".ui-table-expand-btn")[0];
        Assert.Equal("false", button.GetAttribute("aria-expanded"));
        button.Click();

        Assert.Equal("true", cut.FindAll(".ui-table-expand-btn")[0].GetAttribute("aria-expanded"));
        Assert.Contains("detail-Cora", cut.Find(".ui-table-detail-row").TextContent);

        cut.FindAll(".ui-table-expand-btn")[0].Click();
        Assert.Empty(cut.FindAll(".ui-table-detail-row"));
    }

    [Fact]
    public void Selection_expose_aria_selected_et_des_cases_etiquetees()
    {
        var cut = RenderTable(p => p.Add(c => c.Selectable, true));
        Assert.All(cut.FindAll("input[type=checkbox]"), c => Assert.False(string.IsNullOrWhiteSpace(c.GetAttribute("aria-label"))));

        cut.FindAll("tbody input[type=checkbox]")[0].Change(true);

        Assert.Equal("true", cut.FindAll("tbody tr")[0].GetAttribute("aria-selected"));
        Assert.Equal("false", cut.FindAll("tbody tr")[1].GetAttribute("aria-selected"));
    }

    // ---------- Table : ServerData ----------
    [Fact]
    public async Task ServerData_charge_au_demarrage_puis_a_chaque_tri_et_changement_de_page()
    {
        var states = new List<UiTableState>();
        Task<UiTableData<Person>> Load(UiTableState state, CancellationToken _)
        {
            states.Add(state);
            return Task.FromResult(new UiTableData<Person>(People.Take(2).ToList(), 40));
        }

        var cut = RenderComponent<UiTable<Person>>(p => p
            .Add(c => c.ServerData, Load)
            .Add(c => c.HeaderContent, Header())
            .Add(c => c.RowTemplate, Row)
            .Add(c => c.PageSize, 2));

        cut.WaitForAssertion(() => Assert.Single(states));
        Assert.Equal(1, states[0].Page);
        Assert.Equal(2, states[0].PageSize);
        Assert.Equal(new[] { "Cora", "Abel" }, Names(cut));

        cut.FindAll("th")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, states.Count));
        Assert.Equal(new[] { new UiSortDefinition("age", false) }, states[1].Sorts);

        await cut.InvokeAsync(() => cut.Instance.ReloadServerDataAsync());
        Assert.Equal(3, states.Count);
    }

    [Fact]
    public void ServerData_en_erreur_affiche_l_etat_d_erreur()
    {
        var cut = RenderComponent<UiTable<Person>>(p => p
            .Add(c => c.ServerData, (UiTableState _, CancellationToken __) => Task.FromException<UiTableData<Person>>(new InvalidOperationException("boom")))
            .Add(c => c.HeaderContent, Header())
            .Add(c => c.RowTemplate, Row));

        cut.WaitForAssertion(() => Assert.Contains("boom", cut.Markup));
    }

    [Fact]
    public async Task ServerData_ignore_une_reponse_perimee()
    {
        var first = new TaskCompletionSource<UiTableData<Person>>();
        var calls = 0;
        Task<UiTableData<Person>> Load(UiTableState _, CancellationToken __) =>
            ++calls == 1 ? first.Task : Task.FromResult(new UiTableData<Person>(new[] { People[3] }, 1));

        var cut = RenderComponent<UiTable<Person>>(p => p
            .Add(c => c.ServerData, Load)
            .Add(c => c.HeaderContent, Header())
            .Add(c => c.RowTemplate, Row));
        cut.WaitForAssertion(() => Assert.Equal(1, calls));

        await cut.InvokeAsync(() => cut.Instance.ReloadServerDataAsync());
        first.SetResult(new UiTableData<Person>(People, 4));   // arrive en retard : doit être ignorée

        cut.WaitForAssertion(() => Assert.Equal(new[] { "Dina" }, Names(cut)));
    }

    // ---------- Accessibilité ----------
    private static RenderFragment Tabs() => b =>
    {
        foreach (var (title, disabled) in new[] { ("Un", false), ("Deux", true), ("Trois", false) })
        {
            b.OpenComponent<UiTabPanel>(0);
            b.AddAttribute(1, "Title", title);
            b.AddAttribute(2, "Disabled", disabled);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(c => c.AddContent(0, $"contenu-{title}")));
            b.CloseComponent();
        }
    };

    [Fact]
    public void Onglets_exposent_tablist_tab_tabpanel_et_roving_tabindex()
    {
        var cut = RenderComponent<UiTabs>(p => p.Add(c => c.ChildContent, Tabs()).Add(c => c.AriaLabel, "Sections"));

        Assert.Equal("tablist", cut.Find(".ui-tab-nav").GetAttribute("role"));
        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal(3, tabs.Count);
        Assert.Equal("true", tabs[0].GetAttribute("aria-selected"));
        Assert.Equal("0", tabs[0].GetAttribute("tabindex"));
        Assert.Equal("-1", tabs[2].GetAttribute("tabindex"));

        var panel = cut.Find("[role=tabpanel]");
        Assert.Equal(tabs[0].Id, panel.GetAttribute("aria-labelledby"));
        Assert.Equal(panel.Id, tabs[0].GetAttribute("aria-controls"));
    }

    [Fact]
    public void Onglets_fleche_droite_saute_les_onglets_desactives_et_focalise()
    {
        var cut = RenderComponent<UiTabs>(p => p.Add(c => c.ChildContent, Tabs()));

        cut.FindAll("[role=tab]")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.Equal("true", cut.FindAll("[role=tab]")[2].GetAttribute("aria-selected"));
        Assert.Contains("contenu-Trois", cut.Find("[role=tabpanel]").TextContent);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.focusById");

        cut.FindAll("[role=tab]")[2].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("true", cut.FindAll("[role=tab]")[0].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Accordeon_utilise_un_bouton_avec_aria_expanded_et_une_region()
    {
        var cut = RenderComponent<UiAccordionItem>(p => p
            .Add(c => c.Title, "Titre")
            .Add(c => c.ChildContent, b => b.AddContent(0, "corps")));

        var header = cut.Find("button.ui-accordion-header");
        Assert.Equal("false", header.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role=region]"));

        header.Click();

        header = cut.Find("button.ui-accordion-header");
        Assert.Equal("true", header.GetAttribute("aria-expanded"));
        var region = cut.Find("[role=region]");
        Assert.Equal(region.Id, header.GetAttribute("aria-controls"));
        Assert.Equal(header.Id, region.GetAttribute("aria-labelledby"));
    }

    private IRenderedComponent<UiRadioGroup<string>> RenderRadios(string? value, Action<string?>? onChange = null) =>
        RenderComponent<UiRadioGroup<string>>(p => p
            .Add(c => c.Label, "Choix")
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => onChange?.Invoke(v)))
            .Add(c => c.ChildContent, b =>
            {
                var seq = 0;
                foreach (var (v, disabled) in new[] { ("a", false), ("b", true), ("c", false) })
                {
                    b.OpenComponent<UiRadioOption<string>>(seq++);
                    b.AddAttribute(seq++, "Value", v);
                    b.AddAttribute(seq++, "Label", v);
                    b.AddAttribute(seq++, "Disabled", disabled);
                    b.CloseComponent();
                }
            }));

    [Fact]
    public void Radios_exposent_radiogroup_radio_aria_checked_et_un_seul_tabstop()
    {
        var cut = RenderRadios("c");
        var group = cut.Find("[role=radiogroup]");
        Assert.Equal(cut.Find(".ui-label").Id, group.GetAttribute("aria-labelledby"));

        var radios = cut.FindAll("[role=radio]");
        Assert.Equal("false", radios[0].GetAttribute("aria-checked"));
        Assert.Equal("true", radios[2].GetAttribute("aria-checked"));
        Assert.Equal("true", radios[1].GetAttribute("aria-disabled"));
        Assert.Equal(new[] { "-1", "-1", "0" }, radios.Select(r => r.GetAttribute("tabindex")).ToArray());
    }

    [Fact]
    public void Radios_sans_selection_le_premier_actif_est_le_tabstop_et_les_fleches_sautent_les_desactives()
    {
        string? selected = null;
        var cut = RenderRadios(null, v => selected = v);
        Assert.Equal("0", cut.FindAll("[role=radio]")[0].GetAttribute("tabindex"));

        cut.FindAll("[role=radio]")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });

        Assert.Equal("c", selected);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.focusById");
    }

    [Fact]
    public void Progression_et_spinner_et_tooltip_exposent_leurs_roles()
    {
        var progress = RenderComponent<UiProgress>(p => p.Add(c => c.Value, 42.5).Add(c => c.Label, "Envoi"));
        var bar = progress.Find("[role=progressbar]");
        Assert.Equal("42.5", bar.GetAttribute("aria-valuenow"));
        Assert.Equal("Envoi", bar.GetAttribute("aria-label"));

        var spinner = RenderComponent<UiSpinner>();
        Assert.Equal("status", spinner.Find("[role=status]").GetAttribute("role"));

        var tooltip = RenderComponent<UiTooltip>(p => p.Add(c => c.Text, "Aide"));
        var tip = tooltip.Find("[role=tooltip]");
        Assert.Equal(tip.Id, tooltip.Find(".ui-tooltip-wrapper").GetAttribute("aria-describedby"));
    }
}
