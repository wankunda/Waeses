# Changelog

Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/). Les versions suivent [SemVer](https://semver.org/lang/fr/).

## [1.2.0]

### Ajouté
- `UiCalendar` et `UiDatePicker`.
- `UiComponentBase` (`Class`, `Style`, attributs libres), `CssBuilder` et `StyleBuilder`.
- `AddWaesesUIServices(o => ...)` avec `UiOptions` (durée et nombre de toasts, textes par défaut).
- `IUiDialogService` / `UiDialogProvider` : afficher n'importe quel composant dans un dialogue et récupérer son résultat (dialogues empilables).
- `UiPopoverProvider` / `UiPopoverPanel` : popovers rendus à la racine, jamais rognés, repositionnés automatiquement.
- `UiTable` : source `ServerData` (tri, recherche et pagination côté serveur, requêtes périmées ignorées), tri multi-colonnes (`MultiSort`, Maj + clic), lignes extensibles (`ChildRowContent`), `RowClass`, `ReloadServerDataAsync()`.
- `UiThemeDefinition.Accessible()` : préréglage dont chaque couleur atteint 4,5:1 (WCAG AA) en texte sur la surface, sur sa teinte claire et sous le texte d'un bouton plein, en modes clair et sombre ; vérifié par des tests de contraste. `UiColorMath.ContrastRatio`.
- `NavigationItem.Group` et `UiNavMenu.HeaderContent` : titres de section dans le menu de navigation et zone d'en-tête (recherche, sélecteur de version…).
- `UiSelect.Items` (avec `ItemText` et `ItemDisabled`) : génère les options depuis une liste de valeurs ; énumérations libellées par `[Description]`.
- `UiSelectOption<TValue>` : option typée pour `UiSelect` (`Value`, `Text`, `Disabled`) ; les éléments `<option>` natifs restent acceptés et peuvent être mélangés.
- `UiDataGrid` / `UiColumn` : grille de données à colonnes déclaratives — tri multi-colonnes, filtres par colonne (11 opérateurs, comparaison typée), recherche globale, menu de masquage des colonnes, regroupement repliable (`GroupBy`), lignes dépliables, sélection, édition de ligne (copie + `OnRowCommit` / `OnRowCancel`), en-tête collant, virtualisation (`Virtualize`) et source serveur (`ServerData` avec `UiGridState`). `UiFilterEvaluator` est public.
- `Variant` (`UiVariant`) sur `UiChip`, `UiBadge`, `UiAlert` et `UiCallout` ; `Color` sur `UiCheckbox`, `UiSwitch`, `UiSlider`, `UiRadioOption` / `UiRadioGroup` ; `Variant` et `Color` sur `UiTagInput`, `UiMultiSelect` et `UiDateRangePicker`.
- `UiThemeDefinition` : thème décrit en C# (palettes claire et sombre avec teintes dérivées automatiquement, typographie, rayon de référence), appliqué par `UiThemeProvider.Definition` ou `UiOptions.Theme`. Les valeurs sont validées (liste blanche) avant d'être injectées dans la feuille de style.
- `Color` (`UiColor`) et `Appearance` / `Variant` (`UiVariant` : Filled, Outlined, Text) : boutons (`UiButton`, `UiButtonIcon`) et champs (`UiInput`, `UiSelect`, `UiTextArea`, `UiCombobox`, `UiAutocomplete`, `UiDatePicker`, `UiDateTimePicker`). Nouveaux paramètres de `UiButton` : `EndIcon`, `FullWidth`.
- `UiForm` : formulaire avec validation globale (`Validate()`, `ResetValidation()`, `SubmitAsync()`, `IsValid`, `Errors`, `IsTouched`, `IsSubmitting`, `OnValidSubmit` / `OnInvalidSubmit`).
- `UiInputBase<TValue>` : base commune de `UiInput`, `UiSelect`, `UiTextArea`, `UiCheckbox`, `UiSwitch`, `UiSlider`, `UiTimePicker`, `UiColorPicker`, `UiCombobox`, `UiAutocomplete`, `UiDatePicker` et `UiDateTimePicker` (`Id`, `Label`, `HelperText`, `ErrorMessage`, `Error`, `Required`, `Disabled`, `ReadOnly`, `Class`, `Style`).
- `Validation` (`Func<TValue?, string?>`) : règle personnalisée, prise en compte par `EditContext.Validate()` et la soumission d'un `EditForm`.
- `Immediate` et `DebounceInterval` sur `UiInput` et `UiTextArea` (mise à jour à la frappe, à la validation du champ, ou après un délai).
- `UiInputMessages` : zone d'erreur/aide partagée ; `aria-invalid`, `aria-describedby` et `aria-required` sur les champs.
- `UiCarousel` : `AutoCycle`, `Loop`, `Transition` (Slide, Fade, None), clavier, glissement tactile, `ItemTemplate`.

### Modifié
- `UiColorMath.Contrast` (et donc la couleur de texte dérivée des thèmes) choisit le texte, blanc ou `#0f172a`, qui a le meilleur rapport de contraste au lieu d'un seuil de luminance fixe : sur un vert ou un ambre, le blanc ne donnait que 2,5:1.
- `--text-muted` : `#606f85` en mode clair (était `#94a3b8`, 2,6:1) et `#8b9ab3` en mode sombre (était `#64748b`, 3,7:1) pour atteindre 4,5:1 sur les surfaces du thème.
- `UiCombobox`, `UiMultiSelect` et `UiAutocomplete` rendent leur liste via `UiPopoverProvider` quand il est présent (jamais rognée), et se ferment au clic extérieur et sur Échap. Sans provider, ils gardent leur rendu en place.
- 57 composants héritent de `UiComponentBase` et acceptent `Style`.
- `UiButton` : un seul rendu pour `<button>` et `<a>` ; un lien désactivé perd son `href` et expose `aria-disabled`.
- `UiTable` : la vue filtrée/triée est calculée une fois par rendu (au lieu de plusieurs) ; la page courante est bornée.
- Accessibilité : `UiTabs` (tablist/tab/tabpanel, flèches), `UiAccordionItem` (bouton, `aria-expanded`), `UiRadioGroup` (radiogroup, flèches), `UiProgress` (progressbar), `UiSpinner` (status), `UiTooltip` (role tooltip, focus clavier), en-têtes de table triables (`aria-sort`, Entrée).
- Les paramètres `durationMs`, `position` (toasts) et `confirmText`, `cancelText` (boîtes de message) sont nullables et suivent `UiOptions`.

### Corrigé
- `aria-checked` (`UiCheckbox`, `UiSwitch`), `aria-expanded` (`UiPopover`, `UiNavMenu`), `aria-pressed` (bouton « afficher le mot de passe ») et `aria-selected` (`UiCombobox`) disparaissaient quand la valeur était `false` : Blazor retire un attribut booléen faux. Ils valent maintenant toujours `"true"` ou `"false"` (un `role="checkbox"` sans `aria-checked` est invalide).
- `UiCodeBlock` : la zone de code défilable est atteignable au clavier (`tabindex="0"`, `role="region"`, nom accessible).
- `IMessageBoxService.ShowAsync` n'affichait pas « Annuler » quand le type n'était pas `Confirm` : une confirmation stylée en `Danger` ou `Warning` ne pouvait pas être refusée. Une confirmation propose toujours d'annuler ; le type ne change que l'icône et la couleur.
- Libérer `UiThemeProvider`, `UiPopover` ou `UiAutocomplete` pendant le prérendu statique levait « JavaScript interop calls cannot be issued at this time » (journalisé en erreur non gérée à chaque requête de page). Le démontage JS tolère désormais prérendu, circuit fermé et annulation.
- `UiSelect` formatait ses valeurs avec la culture courante mais les relisait en culture invariante : un `decimal` (ex. `1,5` en français) ne pouvait plus être sélectionné. Le format est maintenant invariant des deux côtés.
- `UiProgress` n'avait aucun style pour `Secondary` et `Neutral` (barre invisible) ; `UiTimeline` n'en avait que pour trois couleurs ; `UiChip` pour trois ; `UiCallout` pour quatre. Toutes les couleurs sont maintenant couvertes.
- `UiCombobox` et `UiMultiSelect` ne se fermaient pas au clic extérieur.
- `UiDateRangePicker` et `UiTagInput` relient leur message d'erreur au champ (`aria-invalid`, `aria-describedby`).
- `UiCarousel` revenait à la première image à chaque rendu du parent.
- Le verrouillage du défilement de la page n'était pas compté : fermer une modale imbriquée déverrouillait la page.
- Le focus est rendu à l'élément déclencheur à la fermeture d'une modale.
- `UiTh` : une colonne avec `SortKey` seul paraissait triable sans l'être (côté client).

- `UiRadioGroup` / `UiRadioOption` : l'ancienne option restait cochée après un changement de valeur (options non redessinées).
- `UiCopyButton` : l'état « Copié » n'était jamais affiché ; un refus du navigateur (permission) est géré sans annoncer une copie.
- `UiModal` : le focus revient au déclencheur même quand le parent ferme la modale (`IsOpen = false`).
- `UiMultiSelect` : plus de bouton imbriqué dans un bouton (zone conteneur + bouton de bascule, résumé pour lecteurs d'écran) ; inactif, il ne s'ouvre plus.
- `UiCheckbox` / `UiSwitch` : `aria-disabled` quand désactivés ; `UiInputFile` : nom accessible du champ ; `UiAlert` : `CloseLabel` (« Fermer » par défaut).
- `UiImage` : l'image n'est plus en `display: none` pendant le chargement (une image `loading="lazy"` sans boîte de rendu ne se chargeait jamais, ni `FallbackSrc`) ; changer `Src` relance le chargement.

### Notes de migration
- Avec `UiThemeProvider.Definition` (ou `UiOptions.Theme`), les paramètres `Primary`, `Secondary`… ne sont plus appliqués : la définition les remplace.
- Les colonnes triables côté serveur doivent fournir `SortKey` ; côté client, `SortBy`.
- Ajouter `<UiDialogProvider />` et `<UiPopoverProvider />` au layout pour utiliser les dialogues et les popovers déportés. `UiPopover` garde son rendu en place sans provider.
- Lier `css/components-extended.css` avec son propre `<link>` évite une cascade de requêtes.

## [1.1.6]
- `UiMenu`, styles de menus, `UiAvatarMenu` et `UiDropdownMenu` améliorés.
