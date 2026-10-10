using Microsoft.Playwright;
using WaesesUIDocs.Docs;
using static Microsoft.Playwright.Assertions;

namespace WaesesUIDocs.E2E;

[Collection(DocsSiteCollection.Name)]
public sealed class NavigationTests(DocsSiteFixture site)
{
    public static IEnumerable<object[]> Pages() => DocsCatalog.Entries.Select(e => new object[] { e.Href, e.Title });

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Chaque_page_charge_en_WebAssembly_sans_erreur_ni_exemple_en_echec(string href, string title)
    {
        await using var session = await DocsSession.OpenAsync(site, href);

        var heading = (await session.Page.Locator("article.docs-content h1").InnerTextAsync()).Trim();
        Assert.Equal(href.Length == 0 ? "WaesesUI" : title, heading);

        await Expect(session.Page.GetByText("L'exemple a échoué")).ToHaveCountAsync(0);
        Assert.True(await session.Page.Locator("article.docs-content").IsVisibleAsync());
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Un_lien_profond_fonctionne_comme_une_navigation_depuis_l_accueil()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/boutons");

        await Expect(session.Page).ToHaveURLAsync($"{site.BaseUrl}/composants/boutons");
        await Expect(session.Page.Locator("article.docs-content h1")).ToHaveTextAsync("Boutons");
        await Expect(session.Page.Locator("a.nav-link.active")).ToHaveTextAsync("Boutons");
    }

    [Fact]
    public async Task La_navigation_interne_ne_recharge_pas_la_page_et_le_bouton_retour_fonctionne()
    {
        await using var session = await DocsSession.OpenAsync(site);
        await session.Page.EvaluateAsync("window.__marqueur = 42");

        await session.Page.Locator("a.nav-link", new PageLocatorOptions { HasTextString = "Liste de choix" }).ClickAsync();
        await Expect(session.Page.Locator("article.docs-content h1")).ToHaveTextAsync("Liste de choix");
        await Expect(session.Page).ToHaveURLAsync($"{site.BaseUrl}/composants/liste-de-choix");
        Assert.Equal(42, await session.Page.EvaluateAsync<int>("window.__marqueur"));      // pas de rechargement complet

        await session.Page.GoBackAsync();
        await Expect(session.Page.Locator("article.docs-content h1")).ToHaveTextAsync("WaesesUI");
        Assert.Equal(42, await session.Page.EvaluateAsync<int>("window.__marqueur"));
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Le_menu_liste_toutes_les_pages_groupees_par_rubrique()
    {
        await using var session = await DocsSession.OpenAsync(site);

        var links = await session.Page.Locator("aside.nav-drawer a.nav-link").AllInnerTextsAsync();
        Assert.Equal(DocsCatalog.Entries.Count, links.Count);

        // TextContent et non InnerText : le CSS met ces titres en majuscules (text-transform), pas le contenu.
        var groups = (await session.Page.Locator("aside.nav-drawer .nav-group-title").AllTextContentsAsync()).Select(g => g.Trim()).ToArray();
        Assert.Equal(new[] { "Démarrer", "Guides", "Boutons et actions", "Champs de saisie", "Données", "Affichage", "Navigation et mise en page", "Retours et superpositions" }, groups);
    }

    [Fact]
    public async Task La_recherche_filtre_le_menu_sans_accent_ni_casse_et_signale_l_absence_de_resultat()
    {
        await using var session = await DocsSession.OpenAsync(site);
        var search = session.Page.GetByLabel("Rechercher dans la documentation");
        var links = session.Page.Locator("aside.nav-drawer a.nav-link");

        await search.FillAsync("THEME");
        await Expect(links.First).ToHaveTextAsync("Thème");                                         // sans accent ni casse
        Assert.Contains("Thème", await links.AllInnerTextsAsync());

        await search.FillAsync("datagrid");
        await Expect(links).ToHaveCountAsync(1);
        await Expect(links.First).ToHaveTextAsync("Grille de données");

        await search.FillAsync("zzzzinconnu");
        await Expect(links).ToHaveCountAsync(0);
        await Expect(session.Page.GetByRole(AriaRole.Status)).ToContainTextAsync("Aucun résultat");

        await search.FillAsync("");
        await Expect(links).ToHaveCountAsync(DocsCatalog.Entries.Count);
    }

    [Fact]
    public async Task L_adresse_inconnue_affiche_la_page_introuvable_avec_le_layout()
    {
        await using var session = await DocsSession.OpenAsync(site);
        await session.Page.GotoAsync($"{site.BaseUrl}/cette/page/n/existe/pas");

        await Expect(session.Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Page introuvable" })).ToBeVisibleAsync();
        await Expect(session.Page.Locator("aside.nav-drawer")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Le_basculement_de_theme_change_le_mode_et_survit_au_rechargement()
    {
        await using var session = await DocsSession.OpenAsync(site);
        var html = session.Page.Locator("html");
        await Expect(html).ToHaveAttributeAsync("data-theme", "light");

        await session.Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Mode Sombre" }).ClickAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");

        await session.Page.ReloadAsync();
        await session.WaitForReadyAsync();
        await Expect(html).ToHaveAttributeAsync("data-theme", "dark");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Le_systeme_en_mode_sombre_donne_un_site_sombre_au_premier_chargement()
    {
        await using var session = await DocsSession.OpenAsync(site, scheme: ColorScheme.Dark);

        await Expect(session.Page.Locator("html")).ToHaveAttributeAsync("data-theme", "dark");
    }

    [Theory]
    [MemberData(nameof(Pages))]
    public async Task Sur_mobile_aucune_page_ne_deborde_horizontalement(string href, string title)
    {
        _ = title;
        await using var session = await DocsSession.OpenAsync(site, href, width: 390, height: 844);

        var overflow = await session.Page.EvaluateAsync<int>("document.documentElement.scrollWidth - window.innerWidth");
        Assert.True(overflow <= 1, $"/{href} déborde de {overflow}px sur 390px de large.");
    }
}
