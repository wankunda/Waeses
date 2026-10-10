using System.Globalization;
using System.Text;
using WaesesUI;

namespace WaesesUIDocs.Docs;

/// <summary>Une page de la documentation (alimente le menu, la recherche et l'accueil).</summary>
public sealed record DocEntry(string Title, string Href, string Group, string Description, string Keywords = "");

/// <summary>Catalogue des pages de documentation : source unique du menu latéral, de la recherche et de la page d'accueil.</summary>
public static class DocsCatalog
{
    public const string Start = "Démarrer";
    public const string Guides = "Guides";
    public const string Actions = "Boutons et actions";
    public const string Fields = "Champs de saisie";
    public const string Data = "Données";
    public const string Display = "Affichage";
    public const string Navigation = "Navigation et mise en page";
    public const string Overlays = "Retours et superpositions";

    public static IReadOnlyList<DocEntry> Entries { get; } = new DocEntry[]
    {
        new("Introduction", "", Start, "Ce que contient WaesesUI et comment lire cette documentation.", "accueil presentation"),
        new("Installation", "demarrer", Start, "Ajouter le paquet NuGet, enregistrer les services et charger les ressources statiques.", "nuget install services program.cs assets css js"),

        new("Thème", "guides/theme", Guides, "Couleurs claires et sombres, typographie et rayons décrits en C# avec UiThemeDefinition.", "couleur palette sombre dark police rayon themeprovider"),
        new("Formulaires et validation", "guides/formulaires", Guides, "UiForm, DataAnnotations et règles de validation personnalisées sur chaque champ.", "validation editform dataannotations erreur soumission"),

        new("Menus", "composants/menus", Actions, "UiMenu, UiDropdownMenu, UiAvatarMenu et UiCopyButton : menus d'actions et copie dans le presse-papiers.", "menu dropdown avatar copier presse-papiers clipboard actions"),
        new("Boutons", "composants/boutons", Actions, "UiButton et UiButtonIcon : couleur, apparence, tailles, icônes, chargement et liens.", "button bouton icone fab lien href loading"),

        new("Champ de saisie", "composants/champ-de-saisie", Fields, "UiInput : texte, nombres, mots de passe, validation, saisie différée et apparences.", "input texte nombre password debounce variant filled"),
        new("Liste de choix", "composants/liste-de-choix", Fields, "UiSelect et UiSelectOption : options typées, génération depuis une liste et énumérations.", "select option dropdown enum items"),
        new("Cases, interrupteurs et curseurs", "composants/cases-et-curseurs", Fields, "UiCheckbox, UiSwitch, UiRadioGroup et UiSlider : choix binaires, choix exclusifs et plages de valeurs.", "checkbox switch radio slider case cocher interrupteur curseur"),
        new("Listes avancées", "composants/listes-avancees", Fields, "UiCombobox, UiAutocomplete, UiMultiSelect et UiTagInput : recherche, sélection multiple et étiquettes.", "combobox autocomplete multiselect tags etiquettes recherche asynchrone"),
        new("Dates et heures", "composants/dates-et-heures", Fields, "UiCalendar, UiDatePicker, UiDateRangePicker, UiDateTimePicker et UiTimePicker.", "date heure calendrier plage datetime time picker"),
        new("Fichiers, couleur et langue", "composants/autres-champs", Fields, "UiInputFile, UiColorPicker, UiFormField, UiLocaleSwitcher et UiLocalizedText.", "fichier upload couleur color langue locale culture formfield"),
        new("Formulaire", "composants/formulaire", Fields, "UiForm : validation globale, état de soumission, réinitialisation.", "form submit valid invalid reset editcontext"),

        new("Table", "composants/table", Data, "UiTable, UiTh, UiTd et UiPagination : tableau simple avec tri, recherche, sélection, pagination et source serveur.", "table tableau tri pagination recherche selection serveur"),
        new("Grille de données", "composants/grille-de-donnees", Data, "UiDataGrid : tri, filtres, regroupement, édition de ligne, virtualisation et source serveur.", "datagrid grid table tri filtre pagination virtualisation serveur edition"),

        new("Graphiques", "composants/graphiques", Data, "UiBarChart, UiLineChart, UiPieChart et UiDonutChart : graphiques SVG accessibles.", "graphique chart barres courbe camembert anneau svg"),

        new("Avatars, badges et puces", "composants/avatars-badges-puces", Display, "UiAvatar, UiAvatarGroup, UiBadge, UiChip et UiIcon.", "avatar badge chip puce icone statut"),
        new("Cartes et listes", "composants/cartes-et-listes", Display, "UiCard, UiList, UiListItem, UiDivider, UiTimeline et UiCodeBlock.", "carte card liste separateur chronologie timeline code"),
        new("Images et médias", "composants/images", Display, "UiImage, UiImageGallery, UiCarousel, UiLightbox et UiFilePreview.", "image galerie carrousel lightbox fichier apercu"),

        new("Onglets et accordéon", "composants/onglets-et-accordeon", Navigation, "UiTabs, UiTabPanel, UiAccordion et UiAccordionItem : organiser le contenu en sections.", "onglets tabs accordeon accordion sections"),
        new("Navigation", "composants/navigation", Navigation, "UiNavMenu, UiBreadcrumbs et UiStepper : menu latéral, fil d'Ariane et étapes.", "menu lateral navmenu breadcrumbs fil ariane stepper etapes"),
        new("Mise en page", "composants/mise-en-page", Navigation, "UiLayout, UiSimpleLayout et UiThemeProvider : structure de l'application, mode clair/sombre et densité.", "layout simplelayout themeprovider structure entete pied densite"),

        new("Alertes et états", "composants/alertes-et-etats", Overlays, "UiAlert, UiCallout, UiEmptyState et UiErrorState.", "alerte alert callout encadre etat vide erreur"),
        new("Progression et chargement", "composants/progression-et-chargement", Overlays, "UiProgress, UiProgressCircle, UiSpinner, UiSkeleton et UiLoadingOverlay.", "progression progress spinner skeleton chargement overlay"),
        new("Modale, tiroir et infobulle", "composants/modale-et-tiroir", Overlays, "UiModal, UiDrawer et UiTooltip.", "modale modal drawer tiroir tooltip infobulle"),
        new("Dialogues et messages", "composants/dialogues", Overlays, "IUiDialogService, boîtes de message et notifications toast.", "dialog modal toast messagebox confirm snackbar"),
        new("Popover", "composants/popover", Overlays, "UiPopover, UiPopoverPanel et UiPopoverProvider : contenu ancré, jamais rogné.", "popover overlay menu ancre panel")
    };

    /// <summary>
    /// Composants dont l'API est présentée dans la documentation (tableaux générés). Un test vérifie que chacun de leurs
    /// paramètres a une description dans le code source : ajouter un composant ici impose de le documenter.
    /// </summary>
    public static IReadOnlyList<Type> ApiComponents { get; } = new[]
    {
        // Actions
        typeof(UiButton), typeof(UiButtonIcon), typeof(UiMenu), typeof(UiDropdownMenu), typeof(UiAvatarMenu), typeof(UiCopyButton),
        // Champs
        typeof(UiInput<>), typeof(UiTextArea<>), typeof(UiSelect<>), typeof(UiSelectOption<>), typeof(UiForm),
        typeof(UiCheckbox), typeof(UiSwitch), typeof(UiRadioGroup<>), typeof(UiRadioOption<>), typeof(UiSlider),
        typeof(UiCombobox<,>), typeof(UiAutocomplete<,>), typeof(UiMultiSelect<,>), typeof(UiTagInput),
        typeof(UiCalendar), typeof(UiDatePicker), typeof(UiDateRangePicker), typeof(UiDateTimePicker), typeof(UiTimePicker),
        typeof(UiColorPicker<>), typeof(UiInputFile), typeof(UiFormField), typeof(UiInputMessages), typeof(UiLocaleSwitcher), typeof(UiLocalizedText),
        // Données
        typeof(UiTable<>), typeof(UiTh<>), typeof(UiTd), typeof(UiPagination), typeof(UiDataGrid<>), typeof(UiColumn<>),
        typeof(UiBarChart), typeof(UiLineChart), typeof(UiPieChart), typeof(UiDonutChart),
        // Affichage
        typeof(UiAvatar), typeof(UiAvatarGroup), typeof(UiBadge), typeof(UiChip), typeof(UiIcon),
        typeof(UiCard), typeof(UiList<>), typeof(UiListItem), typeof(UiDivider), typeof(UiTimeline), typeof(UiCodeBlock),
        typeof(UiImage), typeof(UiImageGallery), typeof(UiCarousel), typeof(UiLightbox), typeof(UiFilePreview),
        // Navigation et mise en page
        typeof(UiTabs), typeof(UiTabPanel), typeof(UiAccordion), typeof(UiAccordionItem),
        typeof(UiNavMenu), typeof(UiBreadcrumbs), typeof(UiStepper), typeof(UiLayout), typeof(UiSimpleLayout), typeof(UiThemeProvider),
        // Retours et superpositions
        typeof(UiAlert), typeof(UiCallout), typeof(UiEmptyState), typeof(UiErrorState),
        typeof(UiProgress), typeof(UiProgressCircle), typeof(UiSpinner), typeof(UiSkeleton), typeof(UiLoadingOverlay),
        typeof(UiModal), typeof(UiDrawer), typeof(UiTooltip),
        typeof(UiPopover), typeof(UiPopoverPanel), typeof(UiPopoverProvider),
        typeof(UiDialogProvider), typeof(UiToastContainer), typeof(UiMessageBoxProvider)
    };

    /// <summary>
    /// Composants publics volontairement absents de <see cref="ApiComponents"/>, avec la raison. Un test échoue pour tout composant
    /// public qui n'est ni documenté ni listé ici : une exclusion est une décision écrite, pas un oubli.
    /// </summary>
    public static IReadOnlyDictionary<Type, string> ExcludedComponents { get; } = new Dictionary<Type, string>
    {
        [typeof(UiToastItem)] = "Rendu par UiToastContainer pour chaque notification ; il n'est jamais utilisé directement (voir IToastService).",
        [typeof(UiNavMenuItem)] = "Rendu par UiNavMenu pour chaque NavigationItem ; il n'est jamais utilisé directement.",
        [typeof(UiAuthorizeView)] = "Dédié à l'hébergement WinForms (modèle UserSession) : hors du périmètre Blazor documenté ici.",
        [typeof(UiAuthState)] = "Dédié à l'hébergement WinForms (modèle UserSession) : hors du périmètre Blazor documenté ici.",
        [typeof(UiRedirectTo)] = "Compagnon de UiAuthorizeView, dédié à l'hébergement WinForms."
    };

    /// <summary>Entrées dont chaque mot de la requête figure (sans accent ni casse) dans le titre, la description ou les mots-clés.</summary>
    public static IEnumerable<DocEntry> Search(string? query)
    {
        var terms = Normalize(query).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (terms.Length == 0) return Entries;

        return Entries.Where(entry =>
        {
            var haystack = Normalize($"{entry.Title} {entry.Description} {entry.Keywords}");
            return terms.All(haystack.Contains);
        });
    }

    public static IReadOnlyList<NavigationItem> ToNavigation(IEnumerable<DocEntry> entries) => entries
        .GroupBy(e => e.Group)
        .OrderBy(g => GroupOrder(g.Key))
        .SelectMany(g => g.Select(e => new NavigationItem
        {
            Text = e.Title,
            Href = e.Href,
            Group = g.Key,
            Match = e.Href.Length == 0 ? Microsoft.AspNetCore.Components.Routing.NavLinkMatch.All : Microsoft.AspNetCore.Components.Routing.NavLinkMatch.Prefix
        }))
        .ToList();

    private static int GroupOrder(string group) => group switch
    {
        Start => 0,
        Guides => 1,
        Actions => 2,
        Fields => 3,
        Data => 4,
        Display => 5,
        Navigation => 6,
        Overlays => 7,
        _ => 99
    };

    internal static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var decomposed = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
