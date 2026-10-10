# WaesesUI · Documentation

Site de documentation de WaesesUI, en **Blazor WebAssembly** (fichiers statiques, hébergeable sur GitHub Pages).

```powershell
dotnet run --project WaesesUIDocs      # http://localhost:5000
```

## Ce qui est généré, ce qui est écrit

| Élément | Source |
|---|---|
| Tableaux d'API (paramètres, types, valeurs par défaut) | **Générés** : réflexion sur les `[Parameter]` + commentaires `///` de la bibliothèque (`WaesesUI.xml`, embarqué au build) |
| Code des exemples | **Le fichier `.razor` lui-même** (ressource embarquée) : ce qui est affiché est ce qui s'exécute |
| Menu, recherche, accueil | **Générés** depuis `Docs/DocsCatalog.cs` |
| Textes des pages | Écrits dans `Pages/` |

## Ajouter la documentation d'un composant

1. **Décrire ses paramètres dans le code** de la bibliothèque (`/// <summary>…</summary>` au-dessus de chaque `[Parameter]`).
2. Écrire un ou plusieurs **exemples** dans `Examples/<Famille>/` : un composant `.razor` autonome.
3. Ajouter une entrée à `DocsCatalog.Entries` (titre, route, rubrique, description, mots-clés) et le composant à `DocsCatalog.ApiComponents`.
4. Créer la page dans `Pages/` avec `@page` et `<DocPage Href="…">`, des `<DocSection>`, `<DocExample Example="typeof(…)" />` et `<ApiTable Component="typeof(…)" />`.

Les tests (`WaesesUI.Tests`, `Docs*`) vérifient que :

- chaque page du catalogue existe, et inversement ;
- chaque paramètre d'un composant de `ApiComponents` a une description (ajouter un composant impose de le documenter) ;
- chaque exemple a son code embarqué et s'affiche sans exception ;
- aucun lien interne n'est cassé, aucune entité HTML n'est doublement encodée, aucun accent grave littéral n'apparaît hors bloc de code.

## Vérifié dans un vrai navigateur

`WaesesUIDocs.E2E` pilote Chromium (Playwright) sur le site **publié** : démarrage de chaque page en WebAssembly sans erreur, navigation, recherche, composants interactifs, et contrôle d'accessibilité axe-core (WCAG 2.1 A et AA, modes clair et sombre). Voir son [README](../WaesesUIDocs.E2E/README.md).

## Publication

Le workflow `.github/workflows/docs.yml` publie le site sur GitHub Pages à chaque modification de `WaesesUI/` ou `WaesesUIDocs/` sur `main`
(réglage unique : *Settings → Pages → Source = GitHub Actions*). Il adapte la balise `<base href>` au nom du dépôt et ajoute un `404.html`
pour que les liens profonds fonctionnent.

## Limites actuelles

- Tous les composants publics sont documentés (27 pages, paramètres vérifiés par les tests) ; seuls les composants internes ou à usage indirect (`UiToastItem`, `UiNavMenuItem`, `UiAuthorizeView`, `UiAuthState`, `UiRedirectTo`) en sont exclus, avec leur raison dans `DocsCatalog.ExcludedComponents`.
- Seul Chromium est testé, et un contrôle automatique d'accessibilité ne remplace pas un test au clavier ni avec un lecteur d'écran.
