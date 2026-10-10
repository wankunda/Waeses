using System.Xml.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WaesesUIDocs.Docs;

namespace WaesesUI.Tests;

public sealed class DocsInfrastructureTests : TestContext
{
    public DocsInfrastructureTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- Catalogue et recherche ----------
    [Fact]
    public void Le_catalogue_n_a_ni_doublon_ni_lien_vide_de_titre()
    {
        var hrefs = DocsCatalog.Entries.Select(e => e.Href).ToList();
        Assert.Equal(hrefs.Count, hrefs.Distinct().Count());
        Assert.All(DocsCatalog.Entries, e =>
        {
            Assert.False(string.IsNullOrWhiteSpace(e.Title));
            Assert.False(string.IsNullOrWhiteSpace(e.Description));
        });
    }

    [Theory]
    [InlineData("theme", "Thème")]
    [InlineData("THÈME", "Thème")]
    [InlineData("liste choix", "Liste de choix")]
    [InlineData("datagrid", "Grille de données")]
    [InlineData("ancre", "Popover")]
    public void La_recherche_ignore_accents_et_casse_et_exige_tous_les_mots(string query, string expectedTitle)
    {
        Assert.Contains(DocsCatalog.Search(query), e => e.Title == expectedTitle);
    }

    [Fact]
    public void Une_recherche_vide_renvoie_tout_et_un_mot_inconnu_rien()
    {
        Assert.Equal(DocsCatalog.Entries.Count, DocsCatalog.Search("  ").Count());
        Assert.Equal(DocsCatalog.Entries.Count, DocsCatalog.Search(null).Count());
        Assert.Empty(DocsCatalog.Search("zzzzinconnu"));
        Assert.Empty(DocsCatalog.Search("bouton zzzz"));
    }

    [Fact]
    public void La_navigation_regroupe_dans_l_ordre_des_rubriques_et_marque_l_accueil_exact()
    {
        var items = DocsCatalog.ToNavigation(DocsCatalog.Entries);

        Assert.Equal(DocsCatalog.Entries.Count, items.Count);
        Assert.Equal(DocsCatalog.Start, items[0].Group);

        // Les éléments d'un même groupe se suivent (condition d'affichage des titres de groupe).
        var groups = items.Select(i => i.Group).ToList();
        var collapsed = groups.Where((g, i) => i == 0 || g != groups[i - 1]).ToList();
        Assert.Equal(collapsed.Count, collapsed.Distinct().Count());

        Assert.Equal(Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All, items.Single(i => i.Href == "").Match);
        Assert.All(items.Where(i => i.Href != ""), i => Assert.Equal(Microsoft.AspNetCore.Components.Routing.NavLinkMatch.Prefix, i.Match));
    }

    // ---------- Lecture du XML ----------
    [Fact]
    public void ToPlainText_remplace_les_references_et_le_code_par_des_accents_graves()
    {
        var summary = XElement.Parse("<summary>Voir <see cref=\"P:WaesesUI.UiSelect`1.ChildContent\"/> et <c>Items=\"x\"</c>,\n   <paramref name=\"value\"/>.</summary>");

        Assert.Equal("Voir `ChildContent` et `Items=\"x\"`, `value`.", ApiDocs.ToPlainText(summary));
    }

    [Theory]
    [InlineData("P:WaesesUI.UiSelect`1.ChildContent", "ChildContent")]
    [InlineData("T:WaesesUI.UiSelectOption`1", "UiSelectOption")]
    [InlineData("T:System.String", "String")]
    [InlineData("M:WaesesUI.Foo.Bar(System.Int32)", "Bar")]
    [InlineData(null, "")]
    public void ShortName(string? cref, string expected) => Assert.Equal(expected, ApiDocs.ShortName(cref));

    private sealed class Probe
    {
        public string? MaybeText { get; set; }
        public string Text { get; set; } = "";
        public int? MaybeCount { get; set; }
        public Func<Probe, object?>? Selector { get; set; }
        public EventCallback<int> Changed { get; set; }
        public IReadOnlyList<string> Names { get; set; } = Array.Empty<string>();
        public decimal[]? Amounts { get; set; }
    }

    [Theory]
    [InlineData(nameof(Probe.MaybeText), "string?")]
    [InlineData(nameof(Probe.Text), "string")]
    [InlineData(nameof(Probe.MaybeCount), "int?")]
    [InlineData(nameof(Probe.Selector), "Func<Probe, object?>?")]
    [InlineData(nameof(Probe.Changed), "EventCallback<int>")]
    [InlineData(nameof(Probe.Names), "IReadOnlyList<string>")]
    [InlineData(nameof(Probe.Amounts), "decimal[]?")]
    public void TypeName_rend_les_types_comme_on_les_ecrit_en_C_sharp(string property, string expected)
    {
        var info = typeof(Probe).GetProperty(property)!;
        var nullability = new System.Reflection.NullabilityInfoContext().Create(info);

        Assert.Equal(expected, ApiDocs.TypeName(info.PropertyType, nullability));
    }

    [Fact]
    public void DisplayName_des_composants_generiques()
    {
        Assert.Equal("UiButton", ApiDocs.DisplayName(typeof(UiButton)));
        Assert.Equal("UiInput<TValue>", ApiDocs.DisplayName(typeof(UiInput<>)));
        Assert.Equal("UiCombobox<TItem, TValue>", ApiDocs.DisplayName(typeof(UiCombobox<,>)));
    }

    // ---------- Paramètres générés ----------
    [Fact]
    public void Les_parametres_d_un_bouton_sont_classes_avec_type_defaut_et_description()
    {
        var parameters = ApiDocs.Parameters(typeof(UiButton)).ToDictionary(p => p.Name);

        Assert.Equal(ApiParameterKind.Event, parameters["OnClick"].Kind);
        Assert.Equal(ApiParameterKind.Content, parameters["ChildContent"].Kind);
        Assert.Equal("UiSize.Md", parameters["Size"].Default);
        Assert.Equal("\"button\"", parameters["Type"].Default);
        Assert.Equal("false", parameters["Disabled"].Default);
        Assert.Equal("UiColor?", parameters["Color"].TypeName);
        Assert.NotEmpty(parameters["Color"].Summary);               // lu dans WaesesUI.xml
        Assert.DoesNotContain("Class", parameters.Keys);            // paramètre commun : omis par défaut
    }

    [Fact]
    public void IncludeCommon_ajoute_Class_Style_et_les_attributs_libres()
    {
        var names = ApiDocs.Parameters(typeof(UiButton), includeCommon: true).Select(p => p.Name).ToList();

        Assert.Contains("Class", names);
        Assert.Contains("Style", names);
        Assert.Contains("AdditionalAttributes", names);
        Assert.True(ApiDocs.HasCommonParameters(typeof(UiButton)));
        Assert.False(ApiDocs.HasCommonParameters(typeof(UiInput<>)));       // champ basé sur InputBase : Class/Style y sont déclarés
    }

    [Fact]
    public void Les_composants_generiques_exposent_aussi_les_parametres_de_leur_base()
    {
        var parameters = ApiDocs.Parameters(typeof(UiInput<>)).ToDictionary(p => p.Name);

        Assert.Contains("PrefixIcon", parameters.Keys);                     // UiInput
        Assert.Contains("Validation", parameters.Keys);                     // UiInputBase
        Assert.Contains("Value", parameters.Keys);                          // InputBase (framework)
        Assert.NotEmpty(parameters["Validation"].Summary);
        Assert.NotEmpty(parameters["Value"].Summary);                       // repli sur les descriptions du framework
        Assert.Equal(ApiParameterKind.Event, parameters["ValueChanged"].Kind);
    }

    [Fact]
    public void ApiTable_rend_les_groupes_et_la_note_des_parametres_communs()
    {
        var cut = RenderComponent<ApiTable>(p => p.Add(c => c.Component, typeof(UiButton)));

        var captions = cut.FindAll("caption").Select(c => c.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Paramètres", "Contenu", "Événements" }, captions);
        Assert.Contains("<code>Color</code>", cut.Markup);
        Assert.Contains("api-note", cut.Markup);
        Assert.Contains("&lt;UiButton&gt;", cut.Markup);
    }

    [Fact]
    public void DocText_transforme_les_accents_graves_en_code()
    {
        var cut = RenderComponent<DocText>(p => p.Add(c => c.Text, "Utilisez `Color` pour changer `UiColor`."));

        Assert.Equal(new[] { "Color", "UiColor" }, cut.FindAll("code").Select(c => c.TextContent).ToArray());
        Assert.Equal("Utilisez Color pour changer UiColor.", cut.Markup.Replace("<code>", "").Replace("</code>", ""));
    }

    // ---------- Sources des exemples ----------
    [Fact]
    public void ExampleSource_Clean_retire_BOM_fins_de_ligne_Windows_et_lignes_vides_de_bord()
    {
        Assert.Equal("<UiButton />\n<p>x</p>\n", ExampleSource.Clean("﻿\r\n<UiButton />\r\n<p>x</p>\r\n\r\n"));
    }

    [Fact]
    public void ExampleSource_renvoie_null_pour_un_type_sans_fichier_embarque()
    {
        Assert.Null(ExampleSource.For(typeof(DocsInfrastructureTests)));
    }
}
