using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace WaesesUI.Tests;

public sealed class CalendarTests : TestContext
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public CalendarTests()
    {
        CultureInfo.CurrentCulture = Fr;
        CultureInfo.CurrentUICulture = Fr;
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<UiCalendar> Render(DateTime? value, Action<ComponentParameterCollectionBuilder<UiCalendar>>? extra = null) =>
        RenderComponent<UiCalendar>(p =>
        {
            p.Add(c => c.Value, value);
            extra?.Invoke(p);
        });

    private static AngleSharp.Dom.IElement Day(IRenderedComponent<UiCalendar> cut, string isoDate) =>
        cut.Find($"[data-date='{isoDate}']");

    // ---------- Affichage ----------
    [Fact]
    public void Affiche_le_mois_de_la_valeur_sur_six_semaines()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        Assert.Equal("Octobre 2026", cut.Find(".ui-cal-title").TextContent.Trim());
        Assert.Equal(42, cut.FindAll(".ui-cal-day").Count);
    }

    [Fact]
    public void La_semaine_commence_par_le_lundi_en_francais()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        var premier = cut.FindAll(".ui-cal-weekday")[0];
        Assert.Equal("Lu", premier.TextContent.Trim());
        // 1er octobre 2026 = jeudi : la grille démarre le lundi 28 septembre
        Assert.Equal("2026-09-28", cut.FindAll(".ui-cal-day")[0].GetAttribute("data-date"));
    }

    [Fact]
    public void Le_premier_jour_de_la_semaine_est_parametrable()
    {
        var cut = Render(new DateTime(2026, 10, 9), p => p.Add(c => c.FirstDayOfWeek, DayOfWeek.Sunday));

        Assert.Equal("Di", cut.FindAll(".ui-cal-weekday")[0].TextContent.Trim());
        Assert.Equal("2026-09-27", cut.FindAll(".ui-cal-day")[0].GetAttribute("data-date"));
    }

    [Fact]
    public void Le_jour_choisi_est_marque_comme_selectionne()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        Assert.Equal("true", Day(cut, "2026-10-09").GetAttribute("aria-selected"));
        Assert.Equal("false", Day(cut, "2026-10-10").GetAttribute("aria-selected"));
        Assert.Single(cut.FindAll(".ui-cal-day-selected"));
    }

    // ---------- Sélection ----------
    [Fact]
    public void Cliquer_un_jour_declenche_ValueChanged()
    {
        DateTime? choisi = null;
        var cut = Render(new DateTime(2026, 10, 9),
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => choisi = d)));

        Day(cut, "2026-10-15").Click();

        Assert.Equal(new DateTime(2026, 10, 15), choisi);
        Assert.Equal("true", Day(cut, "2026-10-15").GetAttribute("aria-selected"));
    }

    [Fact]
    public void Cliquer_un_jour_du_mois_voisin_change_de_mois()
    {
        DateTime? choisi = null;
        var cut = Render(new DateTime(2026, 10, 9),
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => choisi = d)));

        Day(cut, "2026-09-30").Click();

        Assert.Equal(new DateTime(2026, 9, 30), choisi);
        Assert.Equal("Septembre 2026", cut.Find(".ui-cal-title").TextContent.Trim());
    }

    [Fact]
    public void Le_bouton_Aujourdhui_selectionne_la_date_du_jour()
    {
        DateTime? choisi = null;
        var cut = Render(null,
            p => p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, d => choisi = d)));

        cut.Find(".ui-cal-today").Click();

        Assert.Equal(DateTime.Today, choisi);
    }

    // ---------- Bornes ----------
    [Fact]
    public void Les_jours_hors_bornes_ne_sont_pas_selectionnables()
    {
        var appels = 0;
        var cut = Render(new DateTime(2026, 10, 15), p =>
        {
            p.Add(c => c.Min, new DateTime(2026, 10, 10));
            p.Add(c => c.Max, new DateTime(2026, 10, 20));
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, _ => appels++));
        });

        Assert.Equal("true", Day(cut, "2026-10-05").GetAttribute("aria-disabled"));
        Assert.Equal("true", Day(cut, "2026-10-25").GetAttribute("aria-disabled"));
        Assert.Null(Day(cut, "2026-10-12").GetAttribute("aria-disabled"));

        Day(cut, "2026-10-05").Click();
        Day(cut, "2026-10-25").Click();
        Assert.Equal(0, appels);

        Day(cut, "2026-10-12").Click();
        Assert.Equal(1, appels);
    }

    [Fact]
    public void IsDateDisabled_bloque_des_dates_precises()
    {
        var appels = 0;
        var cut = Render(new DateTime(2026, 10, 9), p =>
        {
            p.Add(c => c.IsDateDisabled, d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<DateTime?>(this, _ => appels++));
        });

        Day(cut, "2026-10-10").Click();   // samedi
        Assert.Equal(0, appels);
        Day(cut, "2026-10-12").Click();   // lundi
        Assert.Equal(1, appels);
    }

    [Fact]
    public void La_navigation_est_bloquee_aux_bornes()
    {
        var cut = Render(new DateTime(2026, 10, 15), p =>
        {
            p.Add(c => c.Min, new DateTime(2026, 10, 1));
            p.Add(c => c.Max, new DateTime(2026, 11, 30));
        });

        var navs = cut.FindAll(".ui-cal-nav");
        Assert.True(navs[0].HasAttribute("disabled"));    // pas de mois avant octobre
        Assert.False(navs[1].HasAttribute("disabled"));

        navs[1].Click();
        Assert.Equal("Novembre 2026", cut.Find(".ui-cal-title").TextContent.Trim());
        Assert.True(cut.FindAll(".ui-cal-nav")[1].HasAttribute("disabled"));   // pas de mois après novembre
    }

    // ---------- Navigation ----------
    [Fact]
    public void Les_boutons_changent_de_mois_et_d_annee()
    {
        var cut = Render(new DateTime(2026, 12, 31));

        cut.FindAll(".ui-cal-nav")[1].Click();
        Assert.Equal("Janvier 2027", cut.Find(".ui-cal-title").TextContent.Trim());

        cut.FindAll(".ui-cal-nav")[0].Click();
        cut.FindAll(".ui-cal-nav")[0].Click();
        Assert.Equal("Novembre 2026", cut.Find(".ui-cal-title").TextContent.Trim());
    }

    [Fact]
    public void Le_titre_ouvre_la_vue_des_mois_puis_des_annees()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        cut.Find(".ui-cal-title").Click();
        Assert.Equal("2026", cut.Find(".ui-cal-title").TextContent.Trim());
        Assert.Equal(12, cut.FindAll(".ui-cal-pick-item").Count);

        // Choisir mars revient aux jours
        cut.FindAll(".ui-cal-pick-item")[2].Click();
        Assert.Equal("Mars 2026", cut.Find(".ui-cal-title").TextContent.Trim());
        Assert.Equal(42, cut.FindAll(".ui-cal-day").Count);

        // Vue des années : 12 années, choisir une année mène aux mois
        cut.Find(".ui-cal-title").Click();
        cut.Find(".ui-cal-title").Click();
        var annees = cut.FindAll(".ui-cal-pick-item");
        Assert.Equal(12, annees.Count);
        Assert.Equal("2017 – 2028", cut.Find(".ui-cal-title").TextContent.Trim());
        annees.First(a => a.TextContent.Trim() == "2025").Click();
        Assert.Equal("2025", cut.Find(".ui-cal-title").TextContent.Trim());
    }

    // ---------- Clavier ----------
    [Theory]
    [InlineData("ArrowRight", false, "2026-10-10")]
    [InlineData("ArrowLeft", false, "2026-10-08")]
    [InlineData("ArrowDown", false, "2026-10-16")]
    [InlineData("ArrowUp", false, "2026-10-02")]
    [InlineData("Home", false, "2026-10-05")]    // lundi de la semaine
    [InlineData("End", false, "2026-10-11")]     // dimanche de la semaine
    [InlineData("PageDown", false, "2026-11-09")]
    [InlineData("PageUp", false, "2026-09-09")]
    [InlineData("PageDown", true, "2027-10-09")]
    public void Les_touches_deplacent_le_focus(string touche, bool maj, string attendu)
    {
        var cut = Render(new DateTime(2026, 10, 9));

        cut.Find(".ui-cal-grid").KeyDown(new KeyboardEventArgs { Key = touche, ShiftKey = maj });

        var actif = cut.Find(".ui-cal-day[tabindex='0']");
        Assert.Equal(attendu, actif.GetAttribute("data-date"));
    }

    [Fact]
    public void Le_focus_clavier_respecte_les_bornes()
    {
        var cut = Render(new DateTime(2026, 10, 12), p => p.Add(c => c.Min, new DateTime(2026, 10, 10)));

        cut.Find(".ui-cal-grid").KeyDown(new KeyboardEventArgs { Key = "ArrowUp" });   // 5 octobre : sous le minimum

        Assert.Equal("2026-10-10", cut.Find(".ui-cal-day[tabindex='0']").GetAttribute("data-date"));
    }

    [Fact]
    public void Un_seul_jour_est_dans_l_ordre_de_tabulation()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        Assert.Single(cut.FindAll(".ui-cal-day[tabindex='0']"));
    }

    // ---------- Valeur externe ----------
    [Fact]
    public void Une_nouvelle_valeur_deplace_le_calendrier()
    {
        var cut = Render(new DateTime(2026, 10, 9));

        cut.SetParametersAndRender(p => p.Add(c => c.Value, new DateTime(2027, 2, 14)));

        Assert.Equal("Février 2027", cut.Find(".ui-cal-title").TextContent.Trim());
        Assert.Equal("true", Day(cut, "2027-02-14").GetAttribute("aria-selected"));
    }
}
