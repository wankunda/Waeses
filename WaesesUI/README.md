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

### Reference de projet

Depuis une application situee au meme niveau que le dossier `WaesesUI` :

```xml
<ItemGroup>
  <ProjectReference Include="../WaesesUI/WaesesUI.csproj" />
</ItemGroup>
```

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
| Formulaires | `UiInput`, `UiTextArea`, `UiSelect`, `UiMultiSelect`, `UiLocaleSwitcher`, `UiAutocomplete`, `UiDateTimePicker`, `UiCheckbox`, `UiSwitch`, `UiSlider`, `UiRadioGroup`, `UiRadioOption`, `UiInputFile` |
| Contenu | `UiCard`, `UiList`, `UiListItem`, `UiChip`, `UiDivider`, `UiCodeBlock`, `UiCopyButton`, `UiImage`, `UiImageGallery`, `UiLightbox`, `UiFilePreview`, `UiAvatar`, `UiAvatarGroup`, `UiAvatarMenu` |
| Feedback | `UiAlert`, `UiCallout`, `UiTimeline`, `UiLocalizedText`, `UiSpinner`, `UiSkeleton`, `UiEmptyState`, `UiErrorState`, `UiLoadingOverlay`, `UiTooltip`, `UiAccordion`, `UiAccordionItem`, `UiToastContainer`, `UiMessageBoxProvider` |
| Overlays | `UiModal`, `UiPopover`, `UiDropdownMenu`, `UiDrawer` |
| Donnees | `UiTable`, `UiTh`, `UiPagination`, `UiProgress`, `UiProgressCircle` |
| Onglets | `UiTabs`, `UiTabPanel` |
| Icones | `UiIcon` et les collections de la classe `Icons` |

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

## Internationalisation

`AddWaesesUIServices()` enregistre `IUiLocalizationService`. Utilisez `UiLocalizedText` pour un texte traduit, `UiLocaleSwitcher` pour changer de culture et le service pour formater les nombres et les dates :

```razor
<UiLocaleSwitcher />
<UiLocalizedText Key="ui.loading" Fallback="Chargement..." />
```

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

