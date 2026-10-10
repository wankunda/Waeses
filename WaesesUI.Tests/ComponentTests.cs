using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class ComponentTests : TestContext
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public ComponentTests()
    {
        CultureInfo.CurrentCulture = Fr;
        CultureInfo.CurrentUICulture = Fr;
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- UiDatePicker ----------
    private IRenderedComponent<UiDatePicker> RenderPicker(DateTime? value, Action<DateTime?> onChange, Action<ComponentParameterCollectionBuilder<UiDatePicker>>? extra = null) =>
        RenderComponent<UiDatePicker>(p =>
        {
            p.Add(c => c.Value, value);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, onChange));
            p.Add(c => c.ValueExpression, () => value);
            extra?.Invoke(p);
        });

    [Fact]
    public void Le_champ_vide_affiche_le_placeholder_et_pas_de_calendrier()
    {
        var cut = RenderPicker(null, _ => { }, p => p.Add(c => c.Placeholder, "Choisir"));

        Assert.Equal("Choisir", cut.Find(".ui-datepicker-text").TextContent.Trim());
        Assert.Empty(cut.FindAll(".ui-cal"));
        Assert.Empty(cut.FindAll(".ui-datepicker-clear"));
    }

    [Fact]
    public void Le_champ_affiche_la_date_selon_la_culture_ou_le_format()
    {
        var cut = RenderPicker(new DateTime(2026, 10, 9), _ => { });
        Assert.Equal("09/10/2026", cut.Find(".ui-datepicker-text").TextContent.Trim());

        var formate = RenderPicker(new DateTime(2026, 10, 9), _ => { }, p => p.Add(c => c.Format, "dd MMMM yyyy"));
        Assert.Equal("09 octobre 2026", formate.Find(".ui-datepicker-text").TextContent.Trim());
    }

    [Fact]
    public void Cliquer_le_champ_ouvre_le_calendrier_et_choisir_un_jour_le_ferme()
    {
        DateTime? choisi = null;
        var cut = RenderPicker(new DateTime(2026, 10, 9), d => choisi = d);

        cut.Find(".ui-datepicker-trigger").Click();
        Assert.Single(cut.FindAll(".ui-cal"));
        Assert.Equal("true", cut.Find(".ui-datepicker-trigger").GetAttribute("aria-expanded"));

        cut.Find("[data-date='2026-10-20']").Click();

        Assert.Equal(new DateTime(2026, 10, 20), choisi);
        Assert.Empty(cut.FindAll(".ui-cal"));
    }

    [Fact]
    public void Le_bouton_effacer_vide_la_date()
    {
        DateTime? valeur = new DateTime(2026, 10, 9);
        var cut = RenderPicker(valeur, d => valeur = d);

        cut.Find(".ui-datepicker-clear").Click();

        Assert.Null(valeur);
    }

    [Fact]
    public void Un_champ_desactive_ou_en_lecture_seule_ne_s_ouvre_pas()
    {
        var lectureSeule = RenderPicker(new DateTime(2026, 10, 9), _ => { }, p => p.Add(c => c.ReadOnly, true));
        lectureSeule.Find(".ui-datepicker-trigger").Click();
        Assert.Empty(lectureSeule.FindAll(".ui-cal"));
        Assert.Empty(lectureSeule.FindAll(".ui-datepicker-clear"));

        var desactive = RenderPicker(null, _ => { }, p => p.Add(c => c.Disabled, true));
        Assert.True(desactive.Find(".ui-datepicker-trigger").HasAttribute("disabled"));
    }

    [Fact]
    public void Le_message_d_erreur_et_l_aide_sont_affiches()
    {
        var erreur = RenderPicker(null, _ => { }, p => p.Add(c => c.ErrorMessage, "Date obligatoire"));
        Assert.Contains("Date obligatoire", erreur.Find(".ui-error-text").TextContent);
        Assert.Contains("ui-input-error", erreur.Find(".ui-datepicker-trigger").ClassName);

        var aide = RenderPicker(null, _ => { }, p => p.Add(c => c.HelperText, "Jour de paiement"));
        Assert.Contains("Jour de paiement", aide.Find(".ui-helper-text").TextContent);
    }

    [Fact]
    public void Les_bornes_sont_transmises_au_calendrier()
    {
        var cut = RenderPicker(new DateTime(2026, 10, 15), _ => { }, p => p.Add(c => c.Min, new DateTime(2026, 10, 10)));

        cut.Find(".ui-datepicker-trigger").Click();

        Assert.Equal("true", cut.Find("[data-date='2026-10-05']").GetAttribute("aria-disabled"));
    }

    // ---------- UiSelect avec Guid ----------
    [Fact]
    public void UiSelect_gere_les_Guid()
    {
        var premier = Guid.NewGuid();
        var second = Guid.NewGuid();
        var valeur = premier;

        var cut = RenderComponent<UiSelect<Guid>>(p =>
        {
            p.Add(c => c.Value, valeur);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<Guid>(this, v => valeur = v));
            p.Add(c => c.ValueExpression, () => valeur);
            p.AddChildContent($"<option value=\"{premier}\">A</option><option value=\"{second}\">B</option>");
        });

        cut.Find("select").Change(second.ToString());

        Assert.Equal(second, valeur);
    }

    [Fact]
    public void UiSelect_gere_les_Guid_nullables_et_la_valeur_vide()
    {
        var id = Guid.NewGuid();
        Guid? valeur = id;

        var cut = RenderComponent<UiSelect<Guid?>>(p =>
        {
            p.Add(c => c.Value, valeur);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<Guid?>(this, v => valeur = v));
            p.Add(c => c.ValueExpression, () => valeur);
            p.AddChildContent($"<option value=\"\">—</option><option value=\"{id}\">A</option>");
        });

        cut.Find("select").Change("");

        Assert.Null(valeur);
    }

    // ---------- Graphiques ----------
    private static IReadOnlyList<UiChartData> Donnees => [new("Jan", 50000), new("Fév", 1250000)];

    [Fact]
    public void Les_graphiques_gardent_le_format_historique_sans_ValueFormat()
    {
        var cut = RenderComponent<UiBarChart>(p => p.Add(c => c.Data, Donnees));

        Assert.Contains(">1250000<", cut.Markup);
    }

    [Fact]
    public void ValueFormat_met_en_forme_les_valeurs_selon_la_culture()
    {
        var attendu = 1250000d.ToString("N0", Fr);
        var barres = RenderComponent<UiBarChart>(p => p.Add(c => c.Data, Donnees).Add(c => c.ValueFormat, "N0"));
        var lignes = RenderComponent<UiLineChart>(p => p.Add(c => c.Data, Donnees).Add(c => c.ValueFormat, "N0"));

        Assert.Contains(attendu, barres.Markup);
        Assert.Contains(attendu, lignes.Markup);
    }

    [Fact]
    public void ValueFormatter_prime_sur_ValueFormat()
    {
        var cut = RenderComponent<UiBarChart>(p => p
            .Add(c => c.Data, Donnees)
            .Add(c => c.ValueFormat, "N0")
            .Add(c => c.ValueFormatter, v => $"{v / 1000:0.#}k FC"));

        Assert.Contains("1250k FC", cut.Markup);
        Assert.DoesNotContain(1250000d.ToString("N0", Fr), cut.Markup);
    }

    // ---------- Mises en page ----------
    [Fact]
    public void UiSimpleLayout_affiche_le_pied_de_page_par_defaut_et_peut_le_personnaliser_ou_le_masquer()
    {
        var parDefaut = RenderComponent<UiSimpleLayout>();
        Assert.Contains($"© {DateTime.Now.Year} Waeses", parDefaut.Find("footer").TextContent);

        var perso = RenderComponent<UiSimpleLayout>(p => p.Add(c => c.FooterText, "MonBudget 1.0"));
        Assert.Contains("MonBudget 1.0", perso.Find("footer").TextContent);
        Assert.DoesNotContain("Waeses", perso.Find("footer").TextContent);

        var sansTexte = RenderComponent<UiSimpleLayout>(p => p.Add(c => c.FooterText, ""));
        Assert.Empty(sansTexte.FindAll("footer span"));

        var masque = RenderComponent<UiSimpleLayout>(p => p.Add(c => c.ShowFooter, false));
        Assert.Empty(masque.FindAll("footer"));
    }

    [Fact]
    public void UiLayout_peut_masquer_ou_personnaliser_le_pied_de_page()
    {
        var perso = RenderComponent<UiLayout>(p => p.Add(c => c.FooterText, "MonBudget 1.0"));
        Assert.Contains("MonBudget 1.0", perso.Find("footer").TextContent);

        var masque = RenderComponent<UiLayout>(p => p.Add(c => c.ShowFooter, false));
        Assert.Empty(masque.FindAll("footer"));
    }

    [Fact]
    public void L_en_tete_tient_compte_de_la_zone_de_la_barre_d_etat()
    {
        var cut = RenderComponent<UiSimpleLayout>();

        Assert.Contains("safe-area-inset-top", cut.Find("header").GetAttribute("style"));
    }

    // ---------- UiTable ----------
    private sealed record Ligne(string Nom);

    [Fact]
    public void UiTable_pagine_par_defaut_a_10_lignes()
    {
        var lignes = Enumerable.Range(1, 25).Select(i => new Ligne($"L{i}")).ToList();

        var cut = RenderComponent<UiTable<Ligne>>(p => p
            .Add(c => c.Items, lignes)
            .Add(c => c.RowTemplate, (Ligne l) => builder => builder.AddMarkupContent(0, $"<td>{l.Nom}</td>")));

        Assert.Equal(10, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void UiTd_StopPropagation_empeche_le_clic_de_ligne()
    {
        // Le clic sur un bouton d'une cellule marquée StopPropagation exécute le bouton mais n'appelle pas OnRowClick
        Ligne? cliquee = null;
        var actions = 0;
        var lignes = new List<Ligne> { new("A") };

        var cut = RenderComponent<UiTable<Ligne>>(p => p
            .Add(c => c.Items, lignes)
            .Add(c => c.OnRowClick, EventCallback.Factory.Create<Ligne>(this, l => cliquee = l))
            .Add(c => c.RowTemplate, (Ligne l) => builder =>
            {
                builder.OpenComponent<UiTd>(0);
                builder.AddAttribute(1, nameof(UiTd.StopPropagation), true);
                builder.AddAttribute(2, nameof(UiTd.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenElement(0, "button");
                    b.AddAttribute(1, "id", "action");
                    b.AddAttribute(2, "onclick", EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => actions++));
                    b.AddContent(3, "x");
                    b.CloseElement();
                }));
                builder.CloseComponent();
            }));

        // bUnit applique stopPropagation : le clic sur le bouton ne remonte pas à la ligne
        cut.Find("#action").Click();
        Assert.Equal(1, actions);
        Assert.Null(cliquee);

        // Un clic sur la ligne elle-même fonctionne toujours
        cut.Find("tbody tr").Click();
        Assert.Equal(lignes[0], cliquee);
    }

    // ---------- ThemeService ----------
    [Fact]
    public async Task ThemeService_utilise_des_fonctions_dediees_et_plus_eval()
    {
        JSInterop.Setup<string?>("UiInterop.getStoredTheme").SetResult("dark");
        var service = Services.GetRequiredService<IThemeService>();

        await service.InitializeThemeAsync();

        Assert.True(service.IsDarkMode);
        JSInterop.VerifyInvoke("UiInterop.setTheme");
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "eval");
    }

    [Fact]
    public async Task ThemeService_suit_la_preference_du_systeme_sans_theme_enregistre()
    {
        // Aucun thème enregistré : le mode Loose de bUnit renvoie null pour getStoredTheme
        JSInterop.Setup<bool>("UiInterop.prefersDark").SetResult(true);
        var service = Services.GetRequiredService<IThemeService>();

        await service.InitializeThemeAsync();

        Assert.True(service.IsDarkMode);
    }
}
