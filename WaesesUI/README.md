# WaesesUI

WaesesUI est une bibliotheque Razor Class Library pour les applications Blazor. Elle fournit des composants d'interface reutilisables, un theme CSS, des icones SVG, des modeles et des services d'interaction.

## Fonctionnalites

- composants Blazor parametrables ;
- theme clair/sombre avec variables CSS ;
- layouts et navigation responsive ;
- formulaires compatibles avec `EditForm` et la validation Blazor ;
- champs texte et numeriques types avec `UiInput<TValue>` ;
- tables avec recherche, tri, pagination et selection ;
- toasts, alertes, boites de dialogue et modales ;
- icones Material, icones de marques et icones personnalisees ;
- interoperabilite JavaScript limitee aux interactions navigateur necessaires.

## Installation

Vous pouvez installer **WaesesUI** via NuGet en utilisant l'une des méthodes suivantes :

### Via .NET CLI

```bash
dotnet add package WaesesUI
```

### Via Package Manager Console

```powershell
Install-Package WaesesUI
```

### Via une référence de package

```xml
<PackageReference Include="WaesesUI" Version="1.2.0" />
```

### Mise à jour en production

Les composants Razor de WaesesUI sont compilés dans l'application Blazor qui les consomme. Le Hot Reload est une fonctionnalité de développement et ne met pas à jour une application déjà publiée.

Pour déployer une nouvelle version de la bibliothèque, publiez-la sous un nouveau numéro de version, mettez à jour la référence NuGet de l'application, puis reconstruisez et redéployez l'application :

```bash
dotnet add package WaesesUI --version X.Y.Z
dotnet publish -c Release
```

Remplacez `X.Y.Z` par la nouvelle version publiée. Un simple rechargement du navigateur ne remplace pas les assemblies de l'application en production.


### Services

Dans `Program.cs`, enregistrez les services avec l'extension prevue :

```csharp
using WaesesUI;

builder.Services.AddWaesesUIServices();
```

Cette extension enregistre `IToastService`, `IMessageBoxService`, `IUiDialogService` et `IThemeService` avec leurs implementations respectives.

Les valeurs par defaut globales se configurent via `UiOptions` :

```csharp
builder.Services.AddWaesesUIServices(o =>
{
    o.ToastDurationMs = 6000;
    o.MaxToasts = 3;
    o.ConfirmText = "Valider";
});
```

### Assets statiques

Dans `Components/App.razor`, ajoutez le theme et le script d'interoperabilite :

```razor
<link rel="stylesheet" href="_content/WaesesUI/css/theme.css" />
<script src="_content/WaesesUI/js/ui-interop.js"></script>
```

Le script doit etre charge apres le rendu Blazor, generalement juste avant `</body>`.

### Polices

Le theme n'impose plus de telecharger des polices : il s'appuie sur les polices du systeme, ce qui fonctionne hors connexion (application mobile, intranet). Pour retrouver Plus Jakarta Sans et JetBrains Mono, ajoutez en plus du theme (necessite une connexion) :

```razor
<link rel="stylesheet" href="_content/WaesesUI/css/fonts-google.css" />
```

## Imports Razor

Les composants publics utilisent le namespace `WaesesUI` :

```razor
@using WaesesUI
```

Les composants sont organises par familles :

| Famille | Composants |
| --- | --- |
| Actions | `UiButton`, `UiBadge` |
| Layout et navigation | `UiLayout`, `UiSimpleLayout`, `UiNavMenu`, `UiNavMenuItem`, `UiBreadcrumbs`, `UiStepper` |
| Formulaires | `UiFormField`, `UiInput`, `UiTextArea`, `UiSelect`, `UiMultiSelect`, `UiCombobox`, `UiColorPicker`, `UiTimePicker`, `UiDateRangePicker`, `UiTagInput`, `UiLocaleSwitcher`, `UiAutocomplete`, `UiDateTimePicker`, `UiDatePicker`, `UiCalendar`, `UiCheckbox`, `UiSwitch`, `UiSlider`, `UiRadioGroup`, `UiRadioOption`, `UiInputFile` |
| Contenu | `UiCard`, `UiList`, `UiListItem`, `UiChip`, `UiDivider`, `UiCodeBlock`, `UiCopyButton`, `UiImage`, `UiImageGallery`, `UiLightbox`, `UiCarousel`, `UiFilePreview`, `UiAvatar`, `UiAvatarGroup`, `UiAvatarMenu` |
| Feedback | `UiAlert`, `UiCallout`, `UiTimeline`, `UiLocalizedText`, `UiSpinner`, `UiSkeleton`, `UiEmptyState`, `UiErrorState`, `UiLoadingOverlay`, `UiTooltip`, `UiAccordion`, `UiAccordionItem`, `UiToastContainer`, `UiMessageBoxProvider` |
| Overlays | `UiModal`, `UiPopover`, `UiMenu`, `UiDropdownMenu`, `UiDrawer` |
| Donnees | `UiTable`, `UiTh`, `UiPagination`, `UiProgress`, `UiProgressCircle` |
| Onglets | `UiTabs`, `UiTabPanel` |
| Icones | `UiIcon` et les collections de la classe `Icons` |

## Thème et tailles

Placez un unique `UiThemeProvider` dans le layout principal. Il configure globalement le thème, la densité et les palettes de couleurs transmises à tous les composants. Les palettes proviennent de la classe statique `UiColors` et contiennent les teintes `Default`, `Lighten1..5`, `Darken1..4` et, selon la palette, `Accent1..4`.

```razor
<UiThemeProvider Theme="UiTheme.System"
                 Density="UiDensity.Comfortable"
                 Primary="@UiColors.Indigo"
                 Secondary="@UiColors.BlueGray"
                 Success="@UiColors.Green"
                 Danger="@UiColors.Red"
                 Warning="@UiColors.Amber"
                 Info="@UiColors.LightBlue" />
```

Les tailles standard sont `UiSize.Xs`, `Sm`, `Md` et `Lg`. `UiSize.Xl` est conservé pour compatibilité et les éléments volontairement grands. Les tokens typographiques des contrôles sont `--control-font-xs`, `--control-font-sm`, `--control-font-md` et `--control-font-lg`; leurs hauteurs correspondantes sont `--control-height-xs` à `--control-height-lg`. L'espacement s'appuie sur `--space-2xs`, `--space-xs`, `--space-sm`, `--space-md`, `--space-lg` et `--space-xl`. La densité est réglée par `data-ui-density` et `--density-factor`.

Les boutons utilisent `Size`. Les champs natifs peuvent recevoir les classes `ui-input-xs`, `ui-input-sm` et `ui-input-lg` en complément de la taille standard `md`.

## Layout et navigation

`UiLayout` fournit la structure principale avec navigation, en-tete et changement de theme :

```razor
<UiLayout BrandText="Mon application"
          BrandIcon="W"
          VersionText="1.0"
          MainBody="@Body">
    <NavMenu>
        <UiNavMenu BrandText="Mon application"
                   BrandHref="/"
                   NavigationItems="@Items" />
    </NavMenu>
</UiLayout>

@code {
    private static readonly NavigationItem[] Items =
    [
        new() { Text = "Accueil", Href = "/", Match = NavLinkMatch.All },
        new() { Text = "Parametres", Href = "/parametres" }
    ];
}
```

Pour un ecran autonome, comme une page de connexion, utilisez `UiSimpleLayout` avec le fragment `MainBody`.

Le pied de page de `UiLayout` et de `UiSimpleLayout` se configure : `ShowFooter="false"` le masque, `FooterText="Mon texte"` remplace le texte par defaut (`FooterText=""` n'affiche que le fragment `Footer`). Les en-tetes et le pied de page tiennent compte des zones systeme (`env(safe-area-inset-*)`), utile avec `viewport-fit=cover` sur mobile.

## Menus

`UiMenu` ouvre un menu contextuel ancre a son declencheur. Il se ferme apres la selection d'un element, a l'exterieur ou avec `Escape`. Les fleches haut/bas parcourent les elements, et `Home`/`End` vont au premier/dernier. Utilisez des boutons ou des liens dans le menu et marquez-les `role="menuitem"` :

```razor
<UiMenu AriaLabel="Actions du document"
        Alignment="UiMenuAlignment.End"
        Dense="true"
        MaxHeight="18rem">
    <Trigger><span>Actions <span aria-hidden="true">▾</span></span></Trigger>
    <ChildContent>
        <button type="button" role="menuitem" @onclick="EditDocument">Modifier</button>
        <a href="/documents" role="menuitem">Voir les documents</a>
    </ChildContent>
</UiMenu>
```

L'etat peut etre controle avec `@bind-IsOpen`. `UiDropdownMenu` reste disponible comme alias retrocompatible de `UiMenu`.

`UiAvatarMenu` utilise ce meme comportement avec un avatar comme declencheur. Il conserve les parametres de `UiAvatar` (`Src`, `Alt`, `Initials`, `Size`, `Shape`, `Status`, `BackgroundColor`) et permet de controler le menu avec `@bind-IsOpen`, `Disabled`, `Alignment`, `Dense` et `MenuAriaLabel`.

## Formulaires

Les controles derives de `InputBase<TValue>` fonctionnent avec `@bind-Value` et `EditForm` :

```razor
<EditForm Model="model" OnValidSubmit="SubmitAsync">
    <DataAnnotationsValidator />

    <UiInput Label="Nom" @bind-Value="model.Name" Required="true" />
    <UiInput TValue="decimal"
             Label="Budget"
             Type="number"
             @bind-Value="model.Budget"
             Min="0"
             Max="100000"
             Step="0.01"
             SuffixIcon="EUR" />

    <UiButton Text="Enregistrer" Type="submit" />
</EditForm>

@code {
    private readonly FormModel model = new();

    private Task SubmitAsync() => Task.CompletedTask;

    private sealed class FormModel
    {
        public string Name { get; set; } = string.Empty;
        public decimal Budget { get; set; }
    }
}
```

`UiInput<TValue>` prend notamment en charge `string`, `int`, `long`, `double`, `decimal` et les types nullable correspondants via `BindConverter`.

Pour un texte long, utilisez `UiTextArea` avec `@bind-Value`, `Rows`, `MaxLength` et `ShowCharacterCount`. `UiMultiSelect` lie une collection typée via `SelectedValues` et `SelectedValuesChanged` :

```razor
<UiMultiSelect TItem="Team" TValue="int"
               Items="@teams"
               @bind-SelectedValues="selectedTeamIds"
               DisplaySelector="team => team.Name"
               ValueSelector="team => team.Id" />
```

`UiSelect<TValue>` accepte les `string`, nombres, enumerations, `Guid`, `DateOnly` et leurs versions nullables.

`UiTable` pagine par defaut par 10 lignes (`PageSize`). Pour mettre des boutons d'action dans une ligne cliquable (`OnRowClick`), marquez leur cellule avec `<UiTd StopPropagation="true">` : le clic sur le bouton ne declenche plus le clic de ligne.

`UiTable` expose aussi `IsLoading`, `ErrorMessage` et `OnRetry` pour afficher les états de chargement et d'erreur sans dupliquer le rendu dans chaque page. `UiPagination` peut être utilisé seul pour les collections chargées côté serveur.

Sur mobile, `UiTable` affiche chaque ligne sous forme de fiche et masque l'en-tête du tableau. Renseignez `Label` sur chaque `UiTd` pour afficher le nom de la colonne à côté de sa valeur dans cette présentation.

## Internationalisation

`AddWaesesUIServices()` enregistre `IUiLocalizationService`. Utilisez `UiLocalizedText` pour un texte traduit, `UiLocaleSwitcher` pour changer de culture et le service pour formater les nombres et les dates :

```razor
<UiLocaleSwitcher />
<UiLocalizedText Key="ui.loading" Fallback="Chargement..." />
```

## Formulaires avancés

`UiTimePicker` lie un `TimeSpan?`, `UiDateRangePicker` lie séparément les bornes `Start` et `End`, et `UiTagInput` expose une collection de chaînes via `@bind-Values`. `UiCombobox` fournit un contrôle de sélection unique avec filtrage clavier ; `UiColorPicker` lie une couleur hexadécimale. `UiFormField` enveloppe un contrôle personnalisé avec un libellé, une aide et une erreur cohérents.

## Dates : calendrier et selecteur

`UiCalendar` affiche un mois sous forme de grille (6 semaines, premier jour de la semaine selon la culture, lundi en francais). Le titre ouvre la vue des mois puis des annees ; le bouton « Aujourd'hui » selectionne la date du jour.

```razor
<UiCalendar @bind-Value="date"
            Min="DateTime.Today"
            Max="DateTime.Today.AddYears(1)"
            IsDateDisabled="d => d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday" />
```

`UiDatePicker` est un champ de formulaire (`InputBase<DateTime?>`) qui ouvre ce calendrier sous le champ. Il gere `Label`, `HelperText`, `ErrorMessage`, `Required`, `Disabled`, `ReadOnly`, `Clearable`, `Format` (par exemple `"dd MMMM yyyy"`), `Min`, `Max`, `IsDateDisabled` et la validation `EditForm` :

```razor
<UiDatePicker Label="Date de paiement" @bind-Value="model.PaidOn" Format="dd MMMM yyyy" Min="DateTime.Today" />
```

Clavier : fleches (jour et semaine), Debut/Fin (semaine), Page precedente/suivante (mois, avec Maj : annee), Entree ou Espace pour choisir, Echap pour fermer. Les deux composants conservent uniquement la date (l'heure est ignoree) ; pour choisir aussi l'heure, utilisez `UiDateTimePicker`.

## Graphiques

Par defaut, `UiBarChart` et `UiLineChart` affichent les valeurs avec le format `0.##` (culture invariante). `ValueFormat` applique un format .NET dans la culture courante (`"N0"` donne « 50 000 » en francais) et `ValueFormatter` accepte une fonction pour les unites ou devises :

```razor
<UiBarChart Data="data" ValueFormat="N0" />
<UiBarChart Data="data" ValueFormatter="v => $@"{v / 1000:0.#} k FC@"" />
```

La marge gauche s'adapte a la largeur des etiquettes de l'axe.

## Feedback et services

Placez les providers une seule fois dans le layout principal :

```razor
<UiToastContainer />
<UiMessageBoxProvider />
<UiDialogProvider />
<UiPopoverProvider />
```

Puis injectez les services dans une page ou un composant :

```razor
@inject IToastService ToastService
@inject IMessageBoxService MessageBoxService

<UiButton Text="Tester" OnClick="ShowFeedback" />

@code {
    private async Task ShowFeedback()
    {
        ToastService.ShowToast("Operation terminee.", ToastType.Success);
        await MessageBoxService.ShowAlertAsync("Information", "Le composant est interactif.");
    }
}
```

Pour une confirmation, utilisez `ShowAsync` et verifiez la valeur booleenne retournee.

### Dialogues avec un composant

`IUiDialogService` affiche n'importe quel composant dans une modale et renvoie son resultat (plusieurs dialogues peuvent etre empiles) :

```razor
@inject IUiDialogService Dialogs

@code {
    private async Task Edit()
    {
        var reference = await Dialogs.ShowAsync<EditUserForm>(
            "Modifier l'utilisateur",
            new UiDialogParameters().With(nameof(EditUserForm.UserId), 42),
            new UiDialogOptions { Size = UiSize.Lg });

        var result = await reference.Result;
        if (!result.Canceled)
        {
            var user = result.GetData<User>();
        }
    }
}
```

Le composant affiche recoit `[CascadingParameter] UiDialogInstance Dialog` et appelle `Dialog.Close(data)` ou `Dialog.Cancel()`.

### Popovers

`UiPopoverProvider` rend les popovers a la racine de la page : ils ne sont plus rognes par un `overflow: hidden`, se repositionnent (retournement haut/bas, bornes de l'ecran) et passent au-dessus des modales. `UiPopover` l'utilise automatiquement ; sans provider il garde son rendu en place.

Pour un popover personnalise, `UiPopoverPanel` s'ancre sur son element parent (ou `AnchorSelector`) :

```razor
<button @onclick="() => _open = !_open">Options
    <UiPopoverPanel @bind-Open="_open" Placement="UiPopoverPlacement.BottomStart" MatchAnchorWidth="true">
        Contenu...
    </UiPopoverPanel>
</button>
```

Clic exterieur et Echap ferment le panneau (`CloseOnOutsideClick`, `CloseOnEscape`). Le contenu est rendu dans le provider : il n'herite pas des `CascadingValue` du point d'usage.

### Carrousel

`UiCarousel` accepte `Loop`, `AutoCycle` / `AutoCycleInterval` (suspendu au survol et au focus), `Transition` (`Slide`, `Fade`, `None`), `ShowArrows` / `ShowBullets` / `ShowCounter`, le glissement tactile (`EnableSwipe`), le clavier (fleches, Debut, Fin) et `ItemTemplate` pour un contenu personnalise. `ActiveIndex` est optionnel : sans liaison, le carrousel garde son propre etat.

### Composants personnalises

Heritez de `UiComponentBase` pour obtenir `Class`, `Style` et les attributs HTML libres, et utilisez `CssBuilder` / `StyleBuilder` pour composer les classes et styles.

## Icones

`UiIcon` recoit le contenu SVG d'une icone et peut etre colore avec `UiColor` :

```razor
<UiIcon Icon="@Icons.Material.Filled.Home"
        Color="UiColor.Primary"
        Size="UiSize.Lg"
        Title="Accueil" />
```

Les collections sont disponibles sous `Icons.Material.Filled`, `Icons.Material.Outlined`, `Icons.Material.Rounded`, `Icons.Material.Sharp`, `Icons.Material.TwoTone` et `Icons.Custom`.

## Compatibilite et limites

- Le projet cible `net9.0` et `net10.0`.
- `UiAuthorizeView`, `UiAuthState` et `UiRedirectTo` sont reserves au projet WinForms selon les contraintes actuelles de la bibliotheque. Ils ne sont donc pas utilises par `WaesesUIDemo`.
- `UiModal`, `UiNavMenu`, `UiAutocomplete` et certains composants de formulaire utilisent JavaScript interop. Lors de la deconnexion d'un circuit Blazor Server, les appels JavaScript de nettoyage peuvent echouer et doivent etre traites comme tels.
- Les assets sous `_content/WaesesUI` sont servis automatiquement par le mecanisme des bibliotheques Razor.
