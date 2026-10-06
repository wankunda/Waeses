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
<PackageReference Include="WaesesUI" Version="1.1.3" />
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

Cette extension enregistre `IToastService`, `IMessageBoxService` et `IThemeService` avec leurs implementations respectives.

### Assets statiques

Dans `Components/App.razor`, ajoutez le theme et le script d'interoperabilite :

```razor
<link rel="stylesheet" href="_content/WaesesUI/css/theme.css" />
<script src="_content/WaesesUI/js/ui-interop.js"></script>
```

Le script doit etre charge apres le rendu Blazor, generalement juste avant `</body>`.

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
| Formulaires | `UiFormField`, `UiInput`, `UiTextArea`, `UiSelect`, `UiMultiSelect`, `UiCombobox`, `UiColorPicker`, `UiTimePicker`, `UiDateRangePicker`, `UiTagInput`, `UiLocaleSwitcher`, `UiAutocomplete`, `UiDateTimePicker`, `UiCheckbox`, `UiSwitch`, `UiSlider`, `UiRadioGroup`, `UiRadioOption`, `UiInputFile` |
| Contenu | `UiCard`, `UiList`, `UiListItem`, `UiChip`, `UiDivider`, `UiCodeBlock`, `UiCopyButton`, `UiImage`, `UiImageGallery`, `UiLightbox`, `UiCarousel`, `UiFilePreview`, `UiAvatar`, `UiAvatarGroup`, `UiAvatarMenu` |
| Feedback | `UiAlert`, `UiCallout`, `UiTimeline`, `UiLocalizedText`, `UiSpinner`, `UiSkeleton`, `UiEmptyState`, `UiErrorState`, `UiLoadingOverlay`, `UiTooltip`, `UiAccordion`, `UiAccordionItem`, `UiToastContainer`, `UiMessageBoxProvider` |
| Overlays | `UiModal`, `UiPopover`, `UiDropdownMenu`, `UiDrawer` |
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

## Feedback et services

Placez les providers une seule fois dans le layout principal :

```razor
<UiToastContainer />
<UiMessageBoxProvider />
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

- Le projet cible actuellement `net10.0`.
- `UiAuthorizeView`, `UiAuthState` et `UiRedirectTo` sont reserves au projet WinForms selon les contraintes actuelles de la bibliotheque. Ils ne sont donc pas utilises par `WaesesUIDemo`.
- `UiModal`, `UiNavMenu`, `UiAutocomplete` et certains composants de formulaire utilisent JavaScript interop. Lors de la deconnexion d'un circuit Blazor Server, les appels JavaScript de nettoyage peuvent echouer et doivent etre traites comme tels.
- Les assets sous `_content/WaesesUI` sont servis automatiquement par le mecanisme des bibliotheques Razor.
