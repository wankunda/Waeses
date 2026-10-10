# WaesesUIDocs.E2E

Tests de bout en bout du site de documentation, dans un **vrai Chromium** (Playwright). Ils complètent les tests de rendu bUnit de `WaesesUI.Tests`, qui ne voient ni le focus, ni le positionnement, ni la virtualisation, ni les minuteries, ni les contrastes.

## Ce qui est testé

| Fichier | Contenu |
|---|---|
| `NavigationTests` | chaque page démarre en WebAssembly sans erreur (console, exception, requête échouée), liens profonds, navigation sans rechargement, bouton retour, menu groupé, recherche, thème clair/sombre persistant, absence de débordement horizontal sur mobile (390 px) |
| `ComponentTests` | bouton en chargement, lien désactivé, validation de champ, saisie différée, liste de choix, formulaire, grille (tri, filtres, serveur, **virtualisation avec 20 000 lignes**, édition), dialogue (résultat, piège du focus, Échap, focus rendu au déclencheur), confirmation et toast, popover (rendu à la racine, reste dans l'écran, ferme à Échap et au clic extérieur, placement), aperçu du thème |
| `AccessibilityTests` | **axe-core** (WCAG 2.0 et 2.1, A et AA) sur chaque page en mode clair et sombre ; échoue sur toute violation grave ou critique |

## Lancer les tests

```powershell
dotnet test WaesesUIDocs.E2E
```

La fixture **publie le site en Release** (comme pour le déploiement, trimming compris), le sert avec un serveur statique local avec repli sur `index.html`, puis pilote Chromium. Elle télécharge Chromium au premier lancement. Comptez environ une minute de publication, puis 1 à 2 minutes de tests.

| Variable | Effet |
|---|---|
| `DOCS_E2E_SITE` | dossier `wwwroot` déjà publié : évite la publication à chaque essai (`dotnet publish WaesesUIDocs -c Release -o out`, puis `DOCS_E2E_SITE=out/wwwroot`) |
| `DOCS_E2E_AXE_STRICT=1` | fait aussi échouer les violations modérées et mineures d'axe (audit) |

## Limites

- Un contrôle automatique d'accessibilité n'attrape qu'une partie des problèmes (contrastes, noms accessibles, rôles ARIA, structure). Il ne remplace ni un test au clavier ni un test avec un lecteur d'écran.
- Seul Chromium est testé.
- Les transitions CSS sont désactivées pendant la mesure des contrastes (axe mesurerait sinon des couleurs en plein fondu).
