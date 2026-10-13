using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace WaesesUIDocs.E2E;

/// <summary>Les composants dans un vrai navigateur : focus, clavier, positionnement, virtualisation, minuteries.</summary>
[Collection(DocsSiteCollection.Name)]
public sealed class ComponentTests(DocsSiteFixture site)
{
    private static LocatorGetByRoleOptions Name(string name) => new() { Name = name };
    private static PageGetByRoleOptions PageName(string name) => new() { Name = name };

    // ---------- Boutons ----------
    [Fact]
    public async Task Un_bouton_en_chargement_est_occupe_puis_redevient_disponible()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/boutons");
        var button = session.Preview("États et liens").Locator(".ui-btn").First;   // son libellé change pendant le chargement

        await button.ClickAsync();

        await Expect(button).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(button).ToBeDisabledAsync();
        await Expect(button).Not.ToHaveAttributeAsync("aria-busy", "true", new() { Timeout = 10_000 });
        await Expect(button).ToBeEnabledAsync();
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Un_lien_desactive_n_est_pas_navigable()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/boutons");
        var example = session.Preview("États et liens");

        // Un lien sans href garde role="link" : c'est ce qui le garde annoncé comme un lien (désactivé) aux lecteurs d'écran.
        var disabled = example.GetByRole(AriaRole.Link, Name("Lien désactivé"));
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).Not.ToHaveAttributeAsync("href", new Regex(".*"));

        var active = example.GetByRole(AriaRole.Link, Name("Ouvrir GitHub"));
        await Expect(active).ToHaveAttributeAsync("href", "https://github.com/wankunda/Waeses");
        await Expect(active).ToHaveAttributeAsync("rel", "noopener noreferrer");
    }

    // ---------- Champs ----------
    [Fact]
    public async Task La_validation_d_un_champ_apparait_apres_modification_et_disparait_quand_la_valeur_devient_valide()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/champ-de-saisie");
        var example = session.Preview("Validation");
        var field = example.GetByLabel("Code promo");

        await Expect(example.GetByRole(AriaRole.Alert)).ToHaveCountAsync(1);                 // l'erreur imposée par l'appelant, seule
        await field.FillAsync("foo");
        await Expect(example.GetByText("Le code doit commencer par WAE-.")).ToBeVisibleAsync();
        await Expect(field).ToHaveAttributeAsync("aria-invalid", "true");

        await field.FillAsync("WAE-42");
        await Expect(example.GetByText("Le code doit commencer par WAE-.")).ToHaveCountAsync(0);
        await Expect(field).Not.ToHaveAttributeAsync("aria-invalid", "true");
    }

    [Fact]
    public async Task La_saisie_differee_attend_la_fin_de_la_frappe()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/champ-de-saisie");
        var example = session.Preview("Saisie différée");
        var applied = example.Locator("p").First;

        await example.GetByLabel("Recherche différée (500 ms)").PressSequentiallyAsync("grille");

        await Expect(applied).Not.ToContainTextAsync("grille", new() { Timeout = 200 });   // pas encore appliquée
        await Expect(applied).ToContainTextAsync("grille");                                // appliquée après le délai
    }

    [Fact]
    public async Task Une_liste_de_choix_met_a_jour_la_valeur_affichee_et_refuse_les_options_desactivees()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/liste-de-choix");
        var options = session.Preview("Options typées");
        var team = options.GetByLabel("Équipe");

        await team.SelectOptionAsync(new SelectOptionValue { Label = "Design" });
        await Expect(options.Locator("p")).ToContainTextAsync("design");
        Assert.True(await team.Locator("option", new() { HasTextString = "Support (complet)" }).GetAttributeAsync("disabled") is not null);

        var items = session.Preview("Générer les options depuis une liste");
        await items.GetByLabel("Priorité (énumération)").SelectOptionAsync(new SelectOptionValue { Label = "Basse" });
        await Expect(items.Locator("p").First).ToContainTextAsync("Low");
    }

    [Fact]
    public async Task Le_formulaire_valide_les_champs_puis_envoie()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/formulaire");
        var form = session.Preview("Validation globale");

        await form.GetByRole(AriaRole.Button, Name("Envoyer")).ClickAsync();
        await Expect(form.GetByText("Le nom est obligatoire.")).ToBeVisibleAsync();
        await Expect(form.GetByText("L'e-mail est obligatoire.")).ToBeVisibleAsync();
        await Expect(form.GetByText("Vous devez accepter les conditions.")).ToBeVisibleAsync();
        await Expect(form.GetByText("Formulaire invalide")).ToBeVisibleAsync();

        await form.GetByLabel("Nom").FillAsync("Alice");
        await form.GetByLabel("E-mail").FillAsync("alice@exemple.fr");
        await form.GetByRole(AriaRole.Checkbox, Name("J'accepte les conditions")).ClickAsync();
        await form.GetByRole(AriaRole.Button, Name("Envoyer")).ClickAsync();

        await Expect(form.GetByText("Merci Alice : formulaire envoyé.")).ToBeVisibleAsync();
        await Expect(form.GetByText("Formulaire valide")).ToBeVisibleAsync();
        Assert.Empty(session.Problems);
    }

    // ---------- Grille de données ----------
    [Fact]
    public async Task La_grille_trie_au_clic_et_filtre_par_colonne()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/grille-de-donnees");
        var grid = session.Preview("Tri, filtres et recherche");
        var firstName = grid.Locator("tbody tr td[data-title='Nom']").First;

        await Expect(firstName).ToHaveTextAsync("Alice A.");
        var header = grid.Locator("thead tr:first-child th", new() { HasTextString = "Nom" });
        await header.ClickAsync();
        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-sort", "descending");
        await Expect(firstName).ToHaveTextAsync("Jules D.");

        await grid.Locator(".ui-datagrid-filter-row th").Nth(1).Locator("input").FillAsync("Design");
        var departments = grid.Locator("tbody tr td[data-title='Service']");
        await Expect(departments).ToHaveCountAsync(8);
        Assert.All(await departments.AllInnerTextsAsync(), d => Assert.Equal("Design", d.Trim()));
    }

    [Fact]
    public async Task La_grille_serveur_charge_puis_interroge_le_serveur_a_chaque_recherche()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/grille-de-donnees");
        var grid = session.Preview("Données côté serveur");
        var rows = grid.Locator("tbody tr td[data-title='Client']");

        await Expect(rows).ToHaveCountAsync(6, new() { Timeout = 10_000 });

        await grid.GetByRole(AriaRole.Searchbox).FillAsync("Atelier");
        // La recherche est différée (300 ms) puis le serveur simulé répond après 400 ms : les anciennes lignes restent affichées entre-temps.
        await Expect(rows.First).ToHaveTextAsync("Atelier Nord", new() { Timeout = 10_000 });
        await Expect(rows).ToHaveCountAsync(6);
        Assert.All(await rows.AllInnerTextsAsync(), c => Assert.Equal("Atelier Nord", c.Trim()));
    }

    [Fact]
    public async Task La_virtualisation_ne_garde_que_les_lignes_visibles_dans_le_DOM_et_suit_le_defilement()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/grille-de-donnees");
        var grid = session.Preview("Grands volumes");
        var rows = grid.Locator("tbody tr td[data-title='Libellé']");
        var scroller = grid.Locator(".ui-datagrid-scroll");

        await Expect(rows.First).ToContainTextAsync("Ligne 1");
        var rendered = await rows.CountAsync();
        Assert.InRange(rendered, 5, 100);                                    // 20 000 lignes, quelques dizaines dans le DOM

        await scroller.EvaluateAsync("el => { el.scrollTop = el.scrollHeight; }");
        await Expect(rows.Last).ToContainTextAsync(new Regex("Ligne 20.?000"));
        Assert.InRange(await rows.CountAsync(), 5, 100);
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task L_edition_de_ligne_enregistre_la_copie_sans_toucher_a_l_original_avant_validation()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/grille-de-donnees");
        var grid = session.Preview("Édition de ligne");

        await grid.GetByRole(AriaRole.Button, Name("Modifier la ligne")).First.ClickAsync();
        await grid.GetByLabel("Nom du produit").FillAsync("Cahier A5");
        await grid.GetByRole(AriaRole.Button, Name("Annuler")).ClickAsync();
        await Expect(grid.Locator("tbody tr td[data-title='Produit']").First).ToHaveTextAsync("Cahier");

        await grid.GetByRole(AriaRole.Button, Name("Modifier la ligne")).First.ClickAsync();
        await grid.GetByLabel("Nom du produit").FillAsync("Cahier A5");
        await grid.GetByRole(AriaRole.Button, Name("Enregistrer")).ClickAsync();
        await Expect(grid.Locator("tbody tr td[data-title='Produit']").First).ToHaveTextAsync("Cahier A5");
        await Expect(grid.Locator("p")).ToContainTextAsync("Enregistré : Cahier A5");
    }

    // ---------- Dialogues ----------
    [Fact]
    public async Task Le_dialogue_renvoie_son_resultat_piege_le_focus_et_le_rend_a_la_fermeture()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var example = session.Preview("Dialogue avec un composant");
        var trigger = example.GetByRole(AriaRole.Button, Name("Renommer le projet"));

        await trigger.ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Renommer le projet"));
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.Locator(":focus")).ToHaveCountAsync(1);                      // le focus est piégé dans le dialogue
        await session.Page.Keyboard.PressAsync("Tab");
        await session.Page.Keyboard.PressAsync("Tab");
        await session.Page.Keyboard.PressAsync("Tab");
        await session.Page.Keyboard.PressAsync("Tab");
        await Expect(dialog.Locator(":focus")).ToHaveCountAsync(1);                      // Tab ne sort pas du dialogue

        await dialog.GetByLabel("Nom du projet").FillAsync("Projet Beta");
        await dialog.GetByRole(AriaRole.Button, Name("Enregistrer")).ClickAsync();

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(example.Locator("p")).ToContainTextAsync("Projet Beta");
        await Expect(trigger).ToBeFocusedAsync();                                        // focus rendu au déclencheur
    }

    [Fact]
    public async Task Echap_annule_le_dialogue_et_la_page_reste_defilable()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var example = session.Preview("Dialogue avec un composant");

        await example.GetByRole(AriaRole.Button, Name("Renommer le projet")).ClickAsync();
        await Expect(session.Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();
        Assert.Equal("hidden", await session.Page.EvaluateAsync<string>("document.body.style.overflow"));   // page verrouillée

        await session.Page.Keyboard.PressAsync("Escape");

        await Expect(session.Page.GetByRole(AriaRole.Dialog)).ToHaveCountAsync(0);
        await Expect(example.Locator("p")).ToContainTextAsync("renommage annulé");
        Assert.Equal(string.Empty, await session.Page.EvaluateAsync<string>("document.body.style.overflow"));
    }

    [Fact]
    public async Task Le_dialogue_en_ligne_s_ouvre_avec_son_titre_riche_et_se_ferme_par_ses_actions()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var example = session.Preview("Dialogue en ligne");

        await example.GetByRole(AriaRole.Button, Name("Voir les conditions")).ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Conditions d'utilisation"));
        await Expect(dialog).ToBeVisibleAsync();

        await dialog.GetByRole(AriaRole.Button, Name("Accepter")).ClickAsync();

        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(example.Locator("p")).ToContainTextAsync("acceptées");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task La_boite_de_message_a_trois_reponses_renvoie_null_avec_Echap()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var example = session.Preview("Boîte de message à trois réponses");

        await example.GetByRole(AriaRole.Button, Name("Quitter l'éditeur")).ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Modifications non enregistrées"));
        await dialog.GetByRole(AriaRole.Button, Name("Ne pas enregistrer")).ClickAsync();
        await Expect(example.Locator("p")).ToContainTextAsync("quitter sans enregistrer");

        await example.GetByRole(AriaRole.Button, Name("Quitter l'éditeur")).ClickAsync();
        await Expect(dialog).ToBeVisibleAsync();
        await session.Page.Keyboard.PressAsync("Escape");
        await Expect(dialog).ToHaveCountAsync(0);
        await Expect(example.Locator("p")).ToContainTextAsync("rester dans l'éditeur");
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Les_options_placent_le_dialogue_en_plein_ecran_et_SetOptions_l_agrandit()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var example = session.Preview("Options d'un dialogue");

        await example.GetByRole(AriaRole.Button, Name("Ouvrir le dialogue")).ClickAsync();
        var dialog = session.Page.GetByRole(AriaRole.Dialog, PageName("Dialogue configurable"));
        await Expect(dialog).ToBeVisibleAsync();
        await dialog.GetByRole(AriaRole.Button, Name("Agrandir")).ClickAsync();
        await Expect(dialog).ToHaveClassAsync(new Regex("ui-modal-xl"));
        await dialog.GetByRole(AriaRole.Button, Name("Fermer")).First.ClickAsync();
        await Expect(dialog).ToHaveCountAsync(0);

        await example.GetByText("Plein écran").ClickAsync();
        await example.GetByRole(AriaRole.Button, Name("Ouvrir le dialogue")).ClickAsync();
        var box = session.Page.Locator(".ui-modal-box");
        await Expect(box).ToHaveClassAsync(new Regex("ui-modal-fullscreen"));
        await box.EvaluateAsync("el => Promise.all(el.getAnimations().map(a => a.finished))");   // l'apparition part d'une échelle de 0,92
        var size = await box.BoundingBoxAsync();
        Assert.Equal(1280, size!.Width, 1.0);
        Assert.Equal(900, size.Height, 1.0);
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task La_confirmation_renvoie_vrai_ou_faux_et_la_notification_apparait()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/dialogues");
        var box = session.Preview("Confirmations et alertes");

        await box.GetByRole(AriaRole.Button, Name("Confirmer une suppression")).ClickAsync();
        await session.Page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, Name("Supprimer")).ClickAsync();
        await Expect(box.Locator("p")).ToContainTextAsync("Fichier supprimé.");

        await box.GetByRole(AriaRole.Button, Name("Confirmer une suppression")).ClickAsync();
        await session.Page.GetByRole(AriaRole.Alertdialog).GetByRole(AriaRole.Button, Name("Annuler")).ClickAsync();
        await Expect(box.Locator("p")).ToContainTextAsync("Suppression annulée.");

        await session.Preview("Notifications").GetByRole(AriaRole.Button, Name("Succès")).ClickAsync();
        // Dans le conteneur de toasts : le même texte figure aussi dans le code affiché de l'exemple.
        await Expect(session.Page.Locator(".ui-toast").GetByText("Enregistré avec succès.")).ToBeVisibleAsync();
        Assert.Empty(session.Problems);
    }

    // ---------- Popover ----------
    [Fact]
    public async Task Le_popover_est_rendu_a_la_racine_reste_dans_l_ecran_et_se_ferme_au_clavier_et_a_l_exterieur()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/popover", width: 900, height: 700);
        var example = session.Preview("Popover avec déclencheur");
        var layer = session.Page.Locator(".ui-popover-layer");

        await example.GetByRole(AriaRole.Button, Name("Mon compte ▾")).ClickAsync();
        await Expect(layer).ToBeVisibleAsync();
        await Expect(example.Locator(".ui-popover-layer")).ToHaveCountAsync(0);          // pas dans l'exemple : rendu par le provider

        var box = (await layer.BoundingBoxAsync())!;
        Assert.True(box.X >= 0 && box.Y >= 0 && box.X + box.Width <= 900 && box.Y + box.Height <= 700, $"Popover hors écran : {box.X},{box.Y} {box.Width}x{box.Height}");

        await session.Page.Keyboard.PressAsync("Escape");
        await Expect(layer).ToHaveCountAsync(0);

        await example.GetByRole(AriaRole.Button, Name("Mon compte ▾")).ClickAsync();
        await Expect(layer).ToBeVisibleAsync();
        await session.Page.Mouse.ClickAsync(5, 400);
        await Expect(layer).ToHaveCountAsync(0);
        Assert.Empty(session.Problems);
    }

    [Fact]
    public async Task Le_panneau_se_place_au_dessus_de_son_ancre_quand_on_le_demande()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/popover", width: 900, height: 900);
        var example = session.Preview("Panneau ancré");
        var opener = example.GetByRole(AriaRole.Button, Name("Ouvrir le panneau"));
        await example.GetByLabel("Position").SelectOptionAsync(new SelectOptionValue { Value = "TopStart" });

        await opener.ClickAsync();
        var layer = session.Page.Locator(".ui-popover-layer");
        await Expect(layer).ToBeVisibleAsync();

        var anchor = (await example.GetByRole(AriaRole.Button, Name("Fermer")).BoundingBoxAsync())!;
        var panel = (await layer.BoundingBoxAsync())!;
        Assert.True(panel.Y + panel.Height <= anchor.Y + 1, $"Le panneau (bas à {panel.Y + panel.Height}) devrait être au-dessus de l'ancre (haut à {anchor.Y}).");
    }

    // ---------- Thème ----------
    [Fact]
    public async Task L_apercu_du_theme_recalcule_les_teintes_et_la_feuille_de_style()
    {
        await using var session = await DocsSession.OpenAsync(site, "guides/theme");
        var preview = session.Preview("Décrire le thème en C#");

        await preview.GetByLabel("Valeur hexadécimale").FillAsync("#ff0000");
        await preview.GetByLabel("Valeur hexadécimale").PressAsync("Tab");

        await Expect(preview.Locator(".ui-code-block")).ToContainTextAsync("--primary:#ff0000");
        await Expect(preview.Locator("[aria-label='Teintes dérivées']")).ToContainTextAsync("#ff0000");
    }
}
