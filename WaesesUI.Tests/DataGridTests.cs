using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class DataGridTests : TestContext
{
    private sealed record Person(string Name, int Age, string City);

    private sealed class Row
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    private static readonly Person[] People =
    {
        new("Cora", 30, "Paris"), new("Abel", 40, "Lyon"), new("Bruno", 30, "Paris"), new("Dina", 20, "Nice"),
    };

    public DataGridTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- Aides ----------
    private static RenderFragment Columns(bool ageHidden = false, EventCallback<bool>? hiddenChanged = null, string? cellText = null) => b =>
    {
        b.OpenComponent<UiColumn<Person>>(0);
        b.AddAttribute(1, "Title", "Nom");
        b.AddAttribute(2, "Key", "name");
        b.AddAttribute(3, "Field", (Func<Person, object?>)(p => p.Name));
        if (cellText != null)
        {
            b.AddAttribute(4, "CellTemplate", (RenderFragment<Person>)(p => c => c.AddContent(0, $"{cellText}-{p.Name}")));
        }
        b.CloseComponent();

        b.OpenComponent<UiColumn<Person>>(10);
        b.AddAttribute(11, "Title", "Âge");
        b.AddAttribute(12, "Key", "age");
        b.AddAttribute(13, "Field", (Func<Person, object?>)(p => p.Age));
        b.AddAttribute(14, "Hidden", ageHidden);
        if (hiddenChanged is { } cb) b.AddAttribute(15, "HiddenChanged", cb);
        b.CloseComponent();

        b.OpenComponent<UiColumn<Person>>(20);
        b.AddAttribute(21, "Title", "Ville");
        b.AddAttribute(22, "Key", "city");
        b.AddAttribute(23, "Field", (Func<Person, object?>)(p => p.City));
        b.CloseComponent();
    };

    private IRenderedComponent<UiDataGrid<Person>> RenderGrid(Action<ComponentParameterCollectionBuilder<UiDataGrid<Person>>>? extra = null, RenderFragment? columns = null, bool paginated = false)
    {
        var cut = RenderComponent<UiDataGrid<Person>>(p =>
        {
            p.Add(c => c.Items, People);
            p.Add(c => c.Columns, columns ?? Columns());
            p.Add(c => c.Paginated, paginated);
            extra?.Invoke(p);
        });
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("thead tr:first-child th")));
        return cut;
    }

    private static string[] Names(IRenderedComponent<UiDataGrid<Person>> cut) =>
        cut.FindAll("tbody tr:not(.ui-table-detail-row):not(.ui-datagrid-group-row) td[data-title=Nom]").Select(td => td.TextContent.Trim()).ToArray();

    private static string[] Headers(IRenderedComponent<UiDataGrid<Person>> cut) =>
        cut.FindAll("thead tr:first-child th").Select(th => th.TextContent.Trim().TrimEnd('⇅', '▲', '▼', '1', '2', '3').Trim()).ToArray();

    // ---------- UiFilterEvaluator ----------
    [Theory]
    [InlineData("Paris", UiFilterOperator.Contains, "ari", true)]
    [InlineData("Paris", UiFilterOperator.Contains, "ARI", true)]
    [InlineData("Paris", UiFilterOperator.StartsWith, "pa", true)]
    [InlineData("Paris", UiFilterOperator.EndsWith, "ris", true)]
    [InlineData("Paris", UiFilterOperator.Equals, "paris", true)]
    [InlineData("Paris", UiFilterOperator.NotEquals, "Lyon", true)]
    [InlineData("Paris", UiFilterOperator.Contains, "", true)]
    [InlineData("", UiFilterOperator.IsEmpty, null, true)]
    [InlineData(null, UiFilterOperator.IsEmpty, null, true)]
    [InlineData("x", UiFilterOperator.IsNotEmpty, null, true)]
    [InlineData("x", UiFilterOperator.IsEmpty, null, false)]
    public void Evaluator_texte(object? value, UiFilterOperator op, string? operand, bool expected)
    {
        Assert.Equal(expected, UiFilterEvaluator.Matches(value, op, operand, CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Theory]
    [InlineData(30, UiFilterOperator.GreaterThan, "20", true)]
    [InlineData(30, UiFilterOperator.GreaterThan, "30", false)]
    [InlineData(30, UiFilterOperator.GreaterThanOrEqual, "30", true)]
    [InlineData(30, UiFilterOperator.LessThan, "100", true)]     // comparaison numérique, pas alphabétique ("30" > "100" en texte)
    [InlineData(30, UiFilterOperator.LessThanOrEqual, "29", false)]
    [InlineData(30, UiFilterOperator.Equals, "30", true)]
    [InlineData(30, UiFilterOperator.Equals, "abc", false)]      // opérande non convertible : retombe sur le texte
    public void Evaluator_nombres(int value, UiFilterOperator op, string operand, bool expected)
    {
        Assert.Equal(expected, UiFilterEvaluator.Matches(value, op, operand, CultureInfo.GetCultureInfo("fr-FR")));
    }

    [Fact]
    public void Evaluator_dates_decimaux_et_booleens()
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        Assert.True(UiFilterEvaluator.Matches(new DateTime(2026, 5, 10), UiFilterOperator.GreaterThan, "01/05/2026", fr));
        Assert.False(UiFilterEvaluator.Matches(new DateTime(2026, 5, 10), UiFilterOperator.LessThan, "01/05/2026", fr));
        Assert.True(UiFilterEvaluator.Matches(12.5m, UiFilterOperator.GreaterThan, "12,4", fr));
        Assert.True(UiFilterEvaluator.Matches(true, UiFilterOperator.Equals, "true", fr));
    }

    // ---------- Rendu, colonnes ----------
    [Fact]
    public void La_grille_rend_les_colonnes_declarees_et_les_lignes()
    {
        var cut = RenderGrid();

        cut.WaitForAssertion(() => Assert.Equal(new[] { "Cora", "Abel", "Bruno", "Dina" }, Names(cut)));
        Assert.Equal(new[] { "Nom", "Âge", "Ville" }, Headers(cut));
        Assert.Equal("3", cut.Find("table").GetAttribute("aria-colcount"));
        Assert.Equal("5", cut.Find("table").GetAttribute("aria-rowcount"));
    }

    [Fact]
    public void Une_colonne_Hidden_est_masquee_et_le_menu_la_reaffiche()
    {
        var shown = new List<bool>();
        var cut = RenderGrid(columns: Columns(ageHidden: true, hiddenChanged: EventCallback.Factory.Create<bool>(this, shown.Add)));
        Assert.Equal(new[] { "Nom", "Ville" }, Headers(cut));

        cut.Find(".ui-popover-trigger").Click();
        cut.FindAll(".ui-datagrid-columns-item input")[1].Change(true);     // coche « Âge »

        Assert.Equal(new[] { "Nom", "Âge", "Ville" }, Headers(cut));
        Assert.Equal(new[] { false }, shown);
    }

    [Fact]
    public void Le_choix_de_l_utilisateur_survit_a_un_re_rendu_du_parent_tant_que_le_parametre_Hidden_ne_change_pas()
    {
        var cut = RenderGrid(columns: Columns(ageHidden: true));
        cut.Find(".ui-popover-trigger").Click();
        cut.FindAll(".ui-datagrid-columns-item input")[1].Change(true);
        Assert.Contains("Âge", Headers(cut));

        cut.SetParametersAndRender(p => p.Add(c => c.Columns, Columns(ageHidden: true)));
        cut.WaitForAssertion(() => Assert.Contains("Âge", Headers(cut)));

        cut.SetParametersAndRender(p => p.Add(c => c.Columns, Columns(ageHidden: false)));
        cut.SetParametersAndRender(p => p.Add(c => c.Columns, Columns(ageHidden: true)));    // le paramètre change : il reprend la main
        cut.WaitForAssertion(() => Assert.DoesNotContain("Âge", Headers(cut)));
    }

    [Fact]
    public void Un_gabarit_de_cellule_modifie_par_le_parent_est_visible_apres_le_re_rendu()
    {
        var cut = RenderGrid(columns: Columns(cellText: "v1"));
        cut.WaitForAssertion(() => Assert.Equal("v1-Cora", Names(cut)[0]));

        cut.SetParametersAndRender(p => p.Add(c => c.Columns, Columns(cellText: "v2")));

        cut.WaitForAssertion(() => Assert.Equal("v2-Cora", Names(cut)[0]));
    }

    // ---------- Tri ----------
    [Fact]
    public void Tri_croissant_decroissant_et_multi_colonnes()
    {
        var cut = RenderGrid(p => p.Add(c => c.MultiSort, true));
        var header = (int i) => cut.FindAll("thead tr:first-child th")[i];

        Assert.Equal("none", header(0).GetAttribute("aria-sort"));
        header(0).Click();
        Assert.Equal(new[] { "Abel", "Bruno", "Cora", "Dina" }, Names(cut));
        Assert.Equal("ascending", header(0).GetAttribute("aria-sort"));
        header(0).Click();
        Assert.Equal(new[] { "Dina", "Cora", "Bruno", "Abel" }, Names(cut));
        Assert.Equal("descending", header(0).GetAttribute("aria-sort"));

        header(1).Click();                                                      // âge seul : remplace
        header(0).Click(new MouseEventArgs { ShiftKey = true });                // puis nom (ajouté)
        Assert.Equal(new[] { "Dina", "Bruno", "Cora", "Abel" }, Names(cut));
        Assert.Contains("<sup>2</sup>", header(0).InnerHtml);
    }

    [Fact]
    public void Sans_MultiSort_Maj_clic_remplace_le_tri_et_Entree_trie()
    {
        var cut = RenderGrid();
        cut.FindAll("thead tr:first-child th")[1].Click();
        cut.FindAll("thead tr:first-child th")[0].Click(new MouseEventArgs { ShiftKey = true });
        Assert.Equal("Abel", Names(cut)[0]);

        cut.FindAll("thead tr:first-child th")[2].KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal("Abel", Names(cut)[0]);                                    // Lyon d'abord
    }

    // ---------- Filtres et recherche ----------
    [Fact]
    public void Filtre_par_colonne_avec_operateur_et_valeur()
    {
        var cut = RenderGrid(p => p.Add(c => c.ShowFilters, true));
        var cityFilter = () => cut.FindAll(".ui-datagrid-filter-row th")[2];

        cityFilter().QuerySelector("input")!.Input("par");
        Assert.Equal(new[] { "Cora", "Bruno" }, Names(cut));
        Assert.Single(cut.Instance.ActiveFilters);

        cityFilter().QuerySelector("select")!.Change(nameof(UiFilterOperator.NotEquals));
        Assert.Equal(new[] { "Cora", "Abel", "Bruno", "Dina" }, Names(cut));     // « Différent de par » : tout
    }

    [Fact]
    public void Filtre_numerique_et_operateurs_sans_valeur()
    {
        var cut = RenderGrid(p => p.Add(c => c.ShowFilters, true));
        var ageFilter = () => cut.FindAll(".ui-datagrid-filter-row th")[1];

        ageFilter().QuerySelector("select")!.Change(nameof(UiFilterOperator.GreaterThan));
        ageFilter().QuerySelector("input")!.Input("25");
        Assert.Equal(new[] { "Cora", "Abel", "Bruno" }, Names(cut));

        ageFilter().QuerySelector("select")!.Change(nameof(UiFilterOperator.IsNotEmpty));
        Assert.Null(ageFilter().QuerySelector("input"));                         // pas de valeur à saisir
        Assert.Equal(4, Names(cut).Length);
    }

    [Fact]
    public void Recherche_globale_sur_les_colonnes_visibles()
    {
        var cut = RenderGrid(p => p.Add(c => c.ShowSearch, true));
        cut.Find("input[type=search]").Input("lyon");
        Assert.Equal(new[] { "Abel" }, Names(cut));
    }

    // ---------- Groupes, détails, sélection ----------
    [Fact]
    public void Regroupement_par_colonne_avec_effectif_et_repli()
    {
        var cut = RenderGrid(p => p.Add(c => c.GroupBy, "city"));

        var groups = cut.FindAll(".ui-datagrid-group-row").Select(g => g.TextContent.Trim()).ToArray();
        Assert.Equal(3, groups.Length);
        Assert.Contains(groups, g => g.Contains("Paris") && g.Contains("(2)"));
        Assert.Equal(4, Names(cut).Length);

        var toggle = cut.FindAll(".ui-datagrid-group-toggle").First(b => b.TextContent.Contains("Paris"));
        Assert.Equal("true", toggle.GetAttribute("aria-expanded"));
        toggle.Click();

        Assert.Equal(new[] { "Abel", "Dina" }, Names(cut));
        Assert.Equal("false", cut.FindAll(".ui-datagrid-group-toggle").First(b => b.TextContent.Contains("Paris")).GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Lignes_depliables_et_selection_exposent_aria()
    {
        var cut = RenderGrid(p => p
            .Add(c => c.Selectable, true)
            .Add(c => c.ChildRowContent, (Person x) => (RenderFragment)(b => b.AddContent(0, $"detail-{x.Name}"))));

        var expand = cut.FindAll(".ui-table-expand-btn")[0];
        Assert.Equal("false", expand.GetAttribute("aria-expanded"));
        expand.Click();
        Assert.Contains("detail-Cora", cut.Find(".ui-table-detail-row").TextContent);

        cut.FindAll("tbody .ui-datagrid-select-col input")[1].Change(true);
        Assert.Equal("true", cut.FindAll("tbody tr[aria-selected]")[1].GetAttribute("aria-selected"));
        Assert.Equal("false", cut.FindAll("tbody tr[aria-selected]")[0].GetAttribute("aria-selected"));
    }

    // ---------- Serveur ----------
    private IRenderedComponent<UiDataGrid<Person>> RenderServerGrid(Func<UiGridState, CancellationToken, Task<UiGridData<Person>>> load) =>
        RenderComponent<UiDataGrid<Person>>(p => p
            .Add(c => c.ServerData, load)
            .Add(c => c.Columns, Columns())
            .Add(c => c.PageSize, 2)
            .Add(c => c.ShowFilters, true)
            .Add(c => c.DebounceMs, 10));

    [Fact]
    public void ServerData_recoit_page_tri_et_filtres()
    {
        var states = new List<UiGridState>();
        var cut = RenderServerGrid((state, _) =>
        {
            states.Add(state);
            return Task.FromResult(new UiGridData<Person>(People.Take(2).ToList(), 40));
        });

        cut.WaitForAssertion(() => Assert.Single(states));
        Assert.Equal(new UiGridState(1, 2, null, Array.Empty<UiSortDefinition>(), Array.Empty<UiFilterDefinition>()).Page, states[0].Page);
        Assert.Equal(2, states[0].PageSize);
        Assert.Equal(new[] { "Cora", "Abel" }, Names(cut));

        cut.FindAll("thead tr:first-child th")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal(2, states.Count));
        Assert.Equal(new[] { new UiSortDefinition("age", false) }, states[1].Sorts);

        cut.FindAll(".ui-datagrid-filter-row th")[2].QuerySelector("input")!.Input("Pa");
        cut.WaitForAssertion(() => Assert.Equal(3, states.Count));
        Assert.Equal(new[] { new UiFilterDefinition("city", UiFilterOperator.Contains, "Pa") }, states[2].Filters);
        Assert.Equal(1, states[2].Page);
    }

    [Fact]
    public async Task ServerData_ignore_une_reponse_perimee_et_affiche_les_erreurs()
    {
        var first = new TaskCompletionSource<UiGridData<Person>>();
        var calls = 0;
        var cut = RenderServerGrid((_, _) => ++calls == 1
            ? first.Task
            : Task.FromResult(new UiGridData<Person>(new[] { People[3] }, 1)));
        cut.WaitForAssertion(() => Assert.Equal(1, calls));

        await cut.InvokeAsync(() => cut.Instance.ReloadServerDataAsync());
        first.SetResult(new UiGridData<Person>(People, 4));                     // en retard : ignorée
        cut.WaitForAssertion(() => Assert.Equal(new[] { "Dina" }, Names(cut)));

        var failing = RenderServerGrid((_, _) => Task.FromException<UiGridData<Person>>(new InvalidOperationException("boom")));
        failing.WaitForAssertion(() => Assert.Contains("boom", failing.Markup));
    }

    // ---------- Édition ----------
    private IRenderedComponent<UiDataGrid<Row>> RenderEditableGrid(List<Row> rows, Action<UiRowEdit<Row>>? onCommit = null, Action<Row>? onCancel = null)
    {
        var cut = RenderComponent<UiDataGrid<Row>>(p => p
            .Add(c => c.Items, rows)
            .Add(c => c.Paginated, false)
            .Add(c => c.EditMode, UiGridEditMode.Row)
            .Add(c => c.OnRowCommit, EventCallback.Factory.Create<UiRowEdit<Row>>(this, e => onCommit?.Invoke(e)))
            .Add(c => c.OnRowCancel, EventCallback.Factory.Create<Row>(this, r => onCancel?.Invoke(r)))
            .Add(c => c.Columns, (RenderFragment)(b =>
            {
                b.OpenComponent<UiColumn<Row>>(0);
                b.AddAttribute(1, "Title", "Nom");
                b.AddAttribute(2, "Field", (Func<Row, object?>)(r => r.Name));
                b.AddAttribute(3, "EditTemplate", (RenderFragment<Row>)(r => c =>
                {
                    c.OpenElement(0, "input");
                    c.AddAttribute(1, "class", "edit-name");
                    c.AddAttribute(2, "value", r.Name);
                    c.AddAttribute(3, "onchange", EventCallback.Factory.Create<ChangeEventArgs>(this, e => r.Name = e.Value?.ToString() ?? ""));
                    c.CloseElement();
                }));
                b.CloseComponent();
            })));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("thead th")));
        return cut;
    }

    [Fact]
    public void Edition_de_ligne_modifie_une_copie_et_valide_avec_original_et_copie()
    {
        UiRowEdit<Row>? committed = null;
        var original = new Row { Name = "Cora", Age = 30 };
        var cut = RenderEditableGrid(new List<Row> { original }, onCommit: e => committed = e);

        cut.Find("button[aria-label='Modifier la ligne']").Click();
        cut.Find(".edit-name").Change("Coralie");

        Assert.Equal("Cora", original.Name);                                    // l'original n'est pas touché pendant l'édition
        cut.Find("button[aria-label=Enregistrer]").Click();

        Assert.NotNull(committed);
        Assert.Same(original, committed!.Original);
        Assert.Equal("Coralie", committed.Edited.Name);
        Assert.NotSame(original, committed.Edited);
        Assert.Empty(cut.FindAll(".edit-name"));                                // retour en lecture
    }

    [Fact]
    public void Annuler_l_edition_rend_l_original_et_commencer_une_autre_ligne_annule_la_precedente()
    {
        var cancelled = new List<string>();
        var rows = new List<Row> { new() { Name = "Cora" }, new() { Name = "Abel" } };
        var cut = RenderEditableGrid(rows, onCancel: r => cancelled.Add(r.Name));

        cut.FindAll("button[aria-label='Modifier la ligne']")[0].Click();
        cut.Find("button[aria-label=Annuler]").Click();
        Assert.Equal(new[] { "Cora" }, cancelled);
        Assert.Empty(cut.FindAll(".edit-name"));

        cut.FindAll("button[aria-label='Modifier la ligne']")[0].Click();
        cut.FindAll("button[aria-label='Modifier la ligne']")[0].Click();       // la 2e ligne devient éditable
        Assert.Equal(new[] { "Cora", "Cora" }, cancelled);
        Assert.Single(cut.FindAll(".edit-name"));
    }

    // ---------- Virtualisation ----------
    [Fact]
    public void Virtualize_desactive_la_pagination_et_borne_la_hauteur()
    {
        var cut = RenderGrid(p => p.Add(c => c.Virtualize, true), paginated: true);

        Assert.Empty(cut.FindAll(".ui-pagination"));
        Assert.Contains("max-height:28rem", cut.Find(".ui-datagrid-scroll").GetAttribute("style"));
        Assert.Contains("ui-datagrid-sticky", cut.Find(".ui-datagrid").ClassName);
        Assert.Equal("0", cut.Find(".ui-datagrid-scroll").GetAttribute("tabindex"));
    }

    [Fact]
    public void Etats_vide_et_chargement()
    {
        var empty = RenderComponent<UiDataGrid<Person>>(p => p.Add(c => c.Items, Array.Empty<Person>()).Add(c => c.Columns, Columns()).Add(c => c.EmptyText, "Rien ici"));
        empty.WaitForAssertion(() => Assert.Contains("Rien ici", empty.Markup));

        var loading = RenderComponent<UiDataGrid<Person>>(p => p.Add(c => c.Items, People).Add(c => c.Columns, Columns()).Add(c => c.IsLoading, true));
        loading.WaitForAssertion(() => Assert.Equal("true", loading.Find("table").GetAttribute("aria-busy")));
    }
}
