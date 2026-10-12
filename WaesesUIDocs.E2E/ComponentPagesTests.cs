using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace WaesesUIDocs.E2E;

/// <summary>
/// Les composants des pages de documentation « composants/... », dans un vrai navigateur.
/// Un test par page : chaque étape vérifie un comportement qu'un test de rendu bUnit ne voit pas (clavier, focus, positionnement, minuteries).
/// </summary>
[Collection(DocsSiteCollection.Name)]
public sealed class ComponentPagesTests(DocsSiteFixture site)
{
    private static LocatorGetByRoleOptions Name(string name) => new() { Name = name };
    private static PageGetByRoleOptions PageName(string name) => new() { Name = name };

    // ---------- Menus ----------
    [Fact]
    public async Task Menus_au_clavier_ouverture_parcours_selection_et_Echap_qui_rend_le_focus()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/menus");
        var example = session.Preview("Menu d'actions");
        var trigger = example.GetByRole(AriaRole.Button, Name("Actions ▾"));
        var menu = session.Page.GetByRole(AriaRole.Menu, PageName("Actions du document"));

        await trigger.FocusAsync();
        await session.Page.Keyboard.PressAsync("ArrowDown");                              // ouvre le menu
        await Expect(menu).ToBeVisibleAsync();
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(menu.Locator(":focus")).ToHaveCountAsync(1);                         // le focus entre dans le menu

        await session.Page.Keyboard.PressAsync("ArrowDown");
        await session.Page.Keyboard.PressAsync("Enter");                                  // choisit l'élément focalisé
        await Expect(menu).ToHaveCountAsync(0);
        await Expect(example.Locator("strong")).Not.ToHaveTextAsync("aucune");

        await trigger.ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
        await session.Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToHaveCountAsync(0);
        await Expect(trigger).ToBeFocusedAsync();                                         // focus rendu au déclencheur
        await Expect(trigger).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Menus_deroulant_avatar_et_copie()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/menus");

        var dropdown = session.Preview("Menu déroulant");
        await dropdown.GetByRole(AriaRole.Button, Name("Exporter ▾")).ClickAsync();
        await session.Page.GetByRole(AriaRole.Menu, PageName("Formats d'export")).GetByRole(AriaRole.Button, Name("CSV")).ClickAsync();
        await Expect(dropdown.Locator("strong")).ToHaveTextAsync("CSV");

        var avatar = session.Preview("Menu d'avatar");
        await avatar.GetByRole(AriaRole.Button).First.ClickAsync();
        await session.Page.GetByRole(AriaRole.Menu, PageName("Menu du profil d'Alice Martin")).GetByRole(AriaRole.Button, Name("Paramètres")).ClickAsync();
        await Expect(avatar.Locator("strong")).ToHaveTextAsync("Paramètres");

        var copy = session.Preview("Copier dans le presse-papiers");
        await copy.GetByRole(AriaRole.Button, Name("Copier le lien")).ClickAsync();
        await Expect(copy.GetByRole(AriaRole.Button, Name("Copier le lien"))).ToContainTextAsync("Lien copié");
        Assert.Equal("https://github.com/wankunda/Waeses", await session.Page.EvaluateAsync<string>("navigator.clipboard.readText()"));
    }

    // ---------- Champs ----------
    [Fact]
    public async Task Cases_interrupteurs_radios_et_curseurs()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/cases-et-curseurs");

        var boxes = session.Preview("Cases à cocher et interrupteurs");
        var newsletter = boxes.GetByRole(AriaRole.Checkbox, Name("Recevoir la lettre d'information"));
        await Expect(newsletter).ToHaveAttributeAsync("aria-checked", "false");
        await newsletter.ClickAsync();
        await Expect(newsletter).ToHaveAttributeAsync("aria-checked", "true");

        var notifications = boxes.GetByRole(AriaRole.Switch, Name("Notifications"));
        await Expect(notifications).ToHaveAttributeAsync("aria-checked", "true");
        await notifications.FocusAsync();
        await session.Page.Keyboard.PressAsync("Space");                                   // un interrupteur se commande à Espace
        await Expect(notifications).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(boxes.GetByRole(AriaRole.Switch, Name("Verrouillé"))).ToHaveAttributeAsync("aria-disabled", "true");

        var radios = session.Preview("Boutons radio");
        var free = radios.GetByRole(AriaRole.Radio, Name("Gratuite"));
        await Expect(free).ToHaveAttributeAsync("aria-checked", "true");
        await free.FocusAsync();
        await session.Page.Keyboard.PressAsync("ArrowRight");                              // passe à « Pro » (la suivante est désactivée)
        await Expect(radios.GetByRole(AriaRole.Radio, Name("Pro"))).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(radios.Locator("p")).ToContainTextAsync("pro");
        await session.Page.Keyboard.PressAsync("ArrowRight");                              // saute « Entreprise » (désactivée) et boucle
        await Expect(radios.GetByRole(AriaRole.Radio, Name("Gratuite"))).ToHaveAttributeAsync("aria-checked", "true");

        var sliders = session.Preview("Curseurs");
        await sliders.GetByLabel("Volume").FillAsync("70");
        await Expect(sliders.Locator(".ui-slider-val").First).ToHaveTextAsync("70%");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Listes_avancees_combobox_autocomplete_multiselect_et_etiquettes()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/listes-avancees");

        var combo = session.Preview("Liste filtrable");
        await combo.GetByLabel("Ville").PressSequentiallyAsync("Lyo");
        await session.Page.GetByRole(AriaRole.Option, PageName("Lyon")).ClickAsync();
        await Expect(combo.Locator("p")).ToContainTextAsync("2");

        var auto = session.Preview("Recherche locale ou asynchrone");
        await auto.GetByLabel("Pays (recherche asynchrone)").PressSequentiallyAsync("ita");
        await session.Page.GetByRole(AriaRole.Option, PageName("Italie")).ClickAsync();           // après la recherche asynchrone simulée
        await Expect(auto.Locator("p")).ToContainTextAsync("IT");

        var multi = session.Preview("Sélection multiple");
        await multi.GetByRole(AriaRole.Button, Name("Compétences")).ClickAsync();
        await session.Page.GetByRole(AriaRole.Option, PageName("Blazor")).ClickAsync();
        await Expect(multi.Locator("p")).ToContainTextAsync("2 sélectionnée(s)");
        await multi.GetByRole(AriaRole.Button, Name("Retirer C#")).ClickAsync();
        await Expect(multi.Locator("p")).ToContainTextAsync("1 sélectionnée(s)");

        var tags = session.Preview("Étiquettes");
        await tags.GetByLabel("Étiquettes", new() { Exact = true }).FillAsync("docs");
        await session.Page.Keyboard.PressAsync("Enter");
        await Expect(tags.Locator("p")).ToContainTextAsync("docs");
        await session.Page.Keyboard.PressAsync("Backspace");                                      // champ vide : retire la dernière
        await Expect(tags.Locator("p")).Not.ToContainTextAsync("docs");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Dates_calendrier_dates_indisponibles_plage_et_heure()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dates-et-heures");

        var picker = session.Preview("Sélecteur de date");
        await picker.GetByRole(AriaRole.Button, Name("Date de livraison")).ClickAsync();
        var calendar = session.Page.GetByRole(AriaRole.Dialog, PageName("Date de livraison"));
        await Expect(calendar).ToBeVisibleAsync();
        Assert.True(await calendar.Locator("[data-date][aria-disabled='true']").CountAsync() > 0, "Aucune date indisponible (week-ends, dates passées).");
        await calendar.Locator("[data-date]:not([aria-disabled='true'])").Last.ClickAsync();
        await Expect(picker.Locator("p")).Not.ToContainTextAsync("aucune");

        var calendarOnly = session.Preview("Calendrier seul");
        await Expect(calendarOnly.Locator("[data-date$='-13'][aria-disabled='true']").First).ToBeVisibleAsync();   // le 13 est indisponible
        await calendarOnly.Locator("[data-date]:not([aria-disabled='true'])").Nth(8).ClickAsync();
        await Expect(calendarOnly.Locator("p")).Not.ToContainTextAsync("aucun");

        var range = session.Preview("Plage de dates");
        await range.GetByLabel("Date de début").FillAsync("2026-05-10");
        await range.GetByLabel("Date de fin").FillAsync("2026-05-14");
        await Expect(range.Locator("p")).ToContainTextAsync("4 nuit(s)");
        await range.GetByLabel("Date de fin").FillAsync("2026-05-01");
        await Expect(range.GetByRole(AriaRole.Alert)).ToContainTextAsync("début");                                // plage inversée : erreur

        var time = session.Preview("Date, heure ou les deux");
        await time.GetByLabel("Rappel").FillAsync("09:30");
        await Expect(time.Locator("p")).ToContainTextAsync("09:30");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Autres_champs_couleur_fichiers_champ_habille_et_langue()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/autres-champs");

        var color = session.Preview("Couleur");
        await color.GetByLabel("Valeur hexadécimale").FillAsync("#ff0000");
        await color.GetByLabel("Valeur hexadécimale").PressAsync("Tab");
        await Expect(color.GetByRole(AriaRole.Img, new() { Name = "Aperçu de la couleur #ff0000" })).ToBeVisibleAsync();

        var files = session.Preview("Import de fichiers");
        await files.Locator("input[type=file]").First.SetInputFilesAsync(new FilePayload { Name = "rapport.pdf", MimeType = "application/pdf", Buffer = Encoding.UTF8.GetBytes("%PDF-1.4") });
        await Expect(files.Locator("p")).ToContainTextAsync("rapport.pdf");

        var field = session.Preview("Habiller un contrôle natif");
        await field.GetByLabel("Quantité").FillAsync("20");
        await field.GetByLabel("Quantité").PressAsync("Tab");
        await Expect(field.GetByRole(AriaRole.Alert)).ToContainTextAsync("comprise entre 1 et 10");
        await field.GetByLabel("Quantité").FillAsync("5");
        await field.GetByLabel("Quantité").PressAsync("Tab");
        await Expect(field.GetByRole(AriaRole.Alert)).ToHaveCountAsync(0);

        var language = session.Preview("Langue");
        await Expect(language.Locator("p").First).ToContainTextAsync("Copier");
        await language.GetByLabel("Langue").SelectOptionAsync(new SelectOptionValue { Value = "en-US" });
        await Expect(language.Locator("p").First).ToContainTextAsync("Copy");
        await Expect(language.Locator("p").First).ToContainTextAsync("Cancel");
        Assert.Empty(session.Problems);
    }

    // ---------- Données ----------
    [Fact]
    public async Task Table_recherche_tri_selection_serveur_et_pagination()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/table");

        var table = session.Preview("Tri, recherche, sélection et pagination");
        var rows = table.Locator("tbody tr");
        await table.GetByRole(AriaRole.Searchbox).FillAsync("lyon");
        await Expect(rows).ToHaveCountAsync(2);
        await table.GetByRole(AriaRole.Searchbox).FillAsync("");
        await table.Locator("thead th", new() { HasTextString = "Âge" }).ClickAsync();
        await Expect(rows.First).ToContainTextAsync("Gaëlle");                                   // 27 ans : le plus jeune d'abord
        await rows.First.GetByRole(AriaRole.Checkbox).CheckAsync();
        await Expect(table.Locator("p")).ToContainTextAsync("1 ligne(s)");

        var server = session.Preview("Données côté serveur");
        await Expect(server.Locator("tbody tr")).ToHaveCountAsync(5, new() { Timeout = 10_000 });
        var first = await server.Locator("tbody tr").First.InnerTextAsync();
        await server.Locator("thead th", new() { HasTextString = "Total" }).ClickAsync();
        await Expect(server.Locator("thead th", new() { HasTextString = "Total" })).ToHaveAttributeAsync("aria-sort", "ascending");
        await Expect(server.Locator("tbody tr").First).Not.ToHaveTextAsync(first, new() { Timeout = 10_000 });

        var pagination = session.Preview("Pagination seule");
        await pagination.GetByRole(AriaRole.Button, Name("Page suivante")).ClickAsync();
        await Expect(pagination.Locator("p")).ToContainTextAsync("Page 2");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Graphiques_sont_des_images_nommees_avec_leurs_infobulles()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/graphiques");
        var charts = session.Preview("Les quatre graphiques");

        var names = await charts.Locator("svg[role=img]").EvaluateAllAsync<string[]>("els => els.map(e => e.getAttribute('aria-label'))");
        Assert.Equal(new[] { "Ventes par jour de la semaine", "Visites par mois", "Répartition des ventes par canal", "72 pour cent de l'objectif atteint" }, names);
        Assert.True(await charts.Locator("svg title").CountAsync() >= 8, "Les points et parts n'ont pas d'infobulle.");
        var box = (await charts.Locator("svg[role=img]").First.BoundingBoxAsync())!;
        Assert.True(box.Width > 100 && box.Height > 50);
        Assert.Empty(session.Problems);
    }

    // ---------- Affichage ----------
    [Fact]
    public async Task Avatars_badges_puces_et_icones()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/avatars-badges-puces");

        var chips = session.Preview("Badges et puces");
        await Expect(chips.Locator(".ui-chip")).ToHaveCountAsync(3);
        await chips.Locator(".ui-chip button").First.ClickAsync();
        await Expect(chips.Locator(".ui-chip")).ToHaveCountAsync(2);
        await chips.Locator(".ui-chip button").First.ClickAsync();
        await chips.Locator(".ui-chip button").First.ClickAsync();
        await Expect(chips.GetByText("Toutes les puces ont été retirées.")).ToBeVisibleAsync();

        var avatars = session.Preview("Avatars");
        await Expect(avatars.Locator(".ui-avatar")).Not.ToHaveCountAsync(0);
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Cartes_listes_chronologie_et_bloc_de_code()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/cartes-et-listes");

        await Expect(session.Preview("Cartes").Locator(".ui-card")).ToHaveCountAsync(2);
        var list = session.Preview("Listes et séparateurs");
        await Expect(list.Locator("ul.ui-list li")).Not.ToHaveCountAsync(0);
        await Expect(list.GetByText("Liste vide")).ToBeVisibleAsync();
        await Expect(session.Preview("Chronologie").GetByText("Livrée")).ToBeVisibleAsync();
        await Expect(session.Preview("Bloc de code").Locator("pre")).ToHaveAttributeAsync("tabindex", "0");   // atteignable au clavier
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Images_repli_galerie_carrousel_visionneuse_et_aperçu()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/images");

        var image = session.Preview("Image");
        await Expect(image.GetByRole(AriaRole.Img, new() { Name = "Image de remplacement" })).ToHaveAttributeAsync("src", new Regex("^data:image/svg"));   // repli sur FallbackSrc

        var carousel = session.Preview("Carrousel").Locator(".ui-carousel").First;
        await Expect(carousel.Locator(".ui-carousel-footer span").First).ToHaveTextAsync("1 / 3");
        await carousel.GetByRole(AriaRole.Button, Name("Image suivante")).ClickAsync();
        await Expect(carousel.Locator(".ui-carousel-footer span").First).ToHaveTextAsync("2 / 3");
        await carousel.FocusAsync();
        await session.Page.Keyboard.PressAsync("End");
        await Expect(carousel.Locator(".ui-carousel-footer span").First).ToHaveTextAsync("3 / 3");

        var lightbox = session.Preview("Visionneuse");
        await lightbox.GetByRole(AriaRole.Button, Name("Agrandir l'image")).ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Image en plein écran"));
        await Expect(dialog).ToBeVisibleAsync();
        // La touche Échap n'est écoutée qu'une fois la modale prête côté JS, qui y place alors le focus : on attend ce focus avant d'appuyer.
        await Expect(dialog.GetByRole(AriaRole.Button, Name("Fermer"))).ToBeFocusedAsync();
        await session.Page.Keyboard.PressAsync("Escape");
        await Expect(session.Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);

        await Expect(session.Preview("Aperçu de fichier").GetByText("rapport-annuel.pdf")).ToBeVisibleAsync();
        Assert.Empty(session.Problems);
    }

    // ---------- Retours et superpositions ----------
    [Fact]
    public async Task Alertes_fermeture_et_etats_vide_erreur()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/alertes-et-etats");

        var alerts = session.Preview("Alertes");
        await Expect(alerts.GetByRole(AriaRole.Alert).Filter(new() { HasTextString = "Astuce" })).ToBeVisibleAsync();
        await alerts.GetByRole(AriaRole.Button, Name("Fermer")).ClickAsync();                     // nom accessible en français
        await Expect(alerts.GetByRole(AriaRole.Alert).Filter(new() { HasTextString = "Astuce" })).ToHaveCountAsync(0);
        await Expect(alerts.GetByRole(AriaRole.Button, Name("Réafficher l'astuce"))).ToBeVisibleAsync();

        var states = session.Preview("États vide et erreur");
        await states.GetByRole(AriaRole.Button, Name("Créer un projet")).ClickAsync();
        await states.GetByRole(AriaRole.Button, Name("Réessayer")).ClickAsync();
        await Expect(states.Locator("p.text-secondary")).ToContainTextAsync("Projets créés : 1");
        await Expect(states.Locator("p.text-secondary")).ToContainTextAsync("tentatives : 1");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Progression_et_chargement()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/progression-et-chargement");

        var progress = session.Preview("Progression");
        var bar = progress.GetByRole(AriaRole.Progressbar, Name("Envoi des fichiers"));
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "40");
        await progress.GetByRole(AriaRole.Button, Name("Avancer de 10 %")).ClickAsync();
        await Expect(bar).ToHaveAttributeAsync("aria-valuenow", "50");

        var loading = session.Preview("Chargement");
        await loading.GetByRole(AriaRole.Button, Name("Simuler un chargement")).ClickAsync();
        await Expect(loading.GetByRole(AriaRole.Status).GetByText("Mise à jour…")).ToBeVisibleAsync();
        await Expect(loading.GetByText("Mise à jour…")).ToHaveCountAsync(0, new() { Timeout = 6_000 });
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Modale_tiroir_et_infobulle()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/modale-et-tiroir");

        var modal = session.Preview("Modale");
        var open = modal.GetByRole(AriaRole.Button, Name("Supprimer le projet"));
        await open.ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Supprimer le projet ?"));
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, Name("Supprimer")).ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(modal.Locator("p")).ToContainTextAsync("supprimé");
        await Expect(open).ToBeFocusedAsync();

        var drawer = session.Preview("Tiroir");
        await drawer.GetByRole(AriaRole.Button, Name("Panneau à gauche")).ClickAsync();
        var panel = session.Page.GetByRole(AriaRole.Dialog, PageName("Filtres de recherche"));
        await Expect(panel).ToBeVisibleAsync();
        var box = (await panel.BoundingBoxAsync())!;
        Assert.True(box.X <= 1, $"Le tiroir « à gauche » devrait être collé au bord gauche (x = {box.X}).");
        await panel.GetByRole(AriaRole.Button, Name("Appliquer")).ClickAsync();
        await Expect(panel).ToHaveCountAsync(0);

        var tooltip = session.Preview("Infobulle");
        await tooltip.GetByRole(AriaRole.Button, Name("Haut")).FocusAsync();                      // au focus clavier, pas seulement au survol
        await Expect(tooltip.GetByRole(AriaRole.Tooltip, new() { Name = "Enregistrer le document" })).ToBeVisibleAsync();
        Assert.Empty(session.Problems);
    }

    // ---------- Navigation ----------
    [Fact]
    public async Task Onglets_au_clavier_et_accordeon()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/onglets-et-accordeon");

        var tabs = session.Preview("Onglets");
        var overview = tabs.GetByRole(AriaRole.Tab, Name("Aperçu"));
        await Expect(overview).ToHaveAttributeAsync("aria-selected", "true");
        await overview.FocusAsync();
        await session.Page.Keyboard.PressAsync("ArrowRight");
        await Expect(tabs.GetByRole(AriaRole.Tab, new() { Name = "Activité" })).ToHaveAttributeAsync("aria-selected", "true");
        await session.Page.Keyboard.PressAsync("ArrowRight");                                     // saute « Archives » (désactivé)
        await Expect(tabs.GetByRole(AriaRole.Tab, Name("Réglages"))).ToHaveAttributeAsync("aria-selected", "true");
        await Expect(tabs.GetByRole(AriaRole.Tabpanel)).ToContainTextAsync("flèches du clavier");
        await session.Page.Keyboard.PressAsync("ArrowRight");                                     // boucle
        await Expect(overview).ToHaveAttributeAsync("aria-selected", "true");

        var accordion = session.Preview("Accordéon");
        var delivery = accordion.GetByRole(AriaRole.Button, Name("Livraison"));
        var returns = accordion.GetByRole(AriaRole.Button, Name("Retours"));
        await Expect(delivery).ToHaveAttributeAsync("aria-expanded", "true");
        await returns.ClickAsync();
        await Expect(returns).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(delivery).ToHaveAttributeAsync("aria-expanded", "false");                    // un seul ouvert à la fois

        var materials = accordion.GetByRole(AriaRole.Button, Name("Matériaux"));
        var dimensions = accordion.GetByRole(AriaRole.Button, Name("Dimensions"));
        await materials.ClickAsync();
        await Expect(materials).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(dimensions).ToHaveAttributeAsync("aria-expanded", "true");                   // AllowMultiple : l'autre reste ouvert
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Navigation_fil_d_ariane_et_etapes()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/navigation");

        await Expect(session.Preview("Fil d'Ariane").Locator("[aria-current='page']")).ToHaveTextAsync("Couleur et apparence");

        var stepper = session.Preview("Étapes");
        await Expect(stepper.Locator("p")).ToContainTextAsync("Étape 2 sur 4");
        await stepper.GetByRole(AriaRole.Button, Name("Suivant")).ClickAsync();
        await Expect(stepper.Locator("p")).ToContainTextAsync("Étape 3 sur 4");
        await stepper.GetByRole(AriaRole.Button, Name("Précédent")).ClickAsync();
        await stepper.GetByRole(AriaRole.Button, Name("Précédent")).ClickAsync();
        await Expect(stepper.GetByRole(AriaRole.Button, Name("Précédent"))).ToBeDisabledAsync();   // première étape
        Assert.Empty(session.Problems);
    }
}
