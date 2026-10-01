# WaesesUI Demo

Projet Blazor Web App .NET 10 qui presente l'utilisation de la bibliotheque Razor `WaesesUI`.

## Parcours

- `/` : vue d'ensemble et parcours recommande.
- `/fondations` : boutons, badges, alertes, toasts, boites de dialogue, onglets, accordeon, avatars, tooltip et progression.
- `/formulaires` : `EditForm`, validation DataAnnotations, champs texte et numériques typés, select, couleur, heure, plages de dates, tags, combobox, switch, checkbox, slider, radios, autocomplete et upload.
- `/donnees` : table avec recherche, tri, pagination et clic de ligne, progression circulaire, image avec fallback, carousel et confirmation.
- `/avances` : navigation avancée, contenu riche, galerie, aperçu de fichiers, `UiThemeProvider` et localisation.

## Lancer le demo

Depuis le dossier racine :

```powershell
dotnet run --project WaesesUIDemo
```

Le projet reference directement `WaesesUI/WaesesUI.csproj`, enregistre les services avec `AddWaesesUIServices()` et charge les assets sous `_content/WaesesUI`.

Les composants `UiAuthorizeView` et `UiAuthState` ne sont pas presentes ici car la bibliotheque les reserve au projet WinForms.
