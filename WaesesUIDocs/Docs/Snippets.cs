namespace WaesesUIDocs.Docs;

/// <summary>Extraits de code affichés dans les guides (texte seul : les exemples vivants, eux, sont de vrais composants).</summary>
public static class Snippets
{
    public const string InstallPackage = "dotnet add package WaesesUI";

    public const string Services = """
        using WaesesUI;

        builder.Services.AddWaesesUIServices();
        """;

    public const string ServicesWithOptions = """
        builder.Services.AddWaesesUIServices(options =>
        {
            options.ToastDurationMs = 6000;   // durée d'affichage des toasts
            options.MaxToasts = 3;            // toasts visibles en même temps
            options.ConfirmText = "Valider";  // libellé par défaut des confirmations
            options.Theme = new UiThemeDefinition { Light = { Primary = "#0d9488" } };
        });
        """;

    public const string StaticAssetsWasm = """
        <!-- wwwroot/index.html (Blazor WebAssembly) ou Components/App.razor (Blazor Web App) -->
        <head>
            ...
            <link rel="stylesheet" href="_content/WaesesUI/css/theme.css" />
            <link rel="stylesheet" href="_content/WaesesUI/css/components-extended.css" />
        </head>
        <body>
            ...
            <script src="_framework/blazor.webassembly.js"></script> <!-- ou blazor.web.js -->
            <script src="_content/WaesesUI/js/ui-interop.js"></script>
        </body>
        """;

    public const string Imports = "@using WaesesUI";

    public const string Providers = """
        @* Layout principal : une seule fois pour toute l'application. *@
        <UiToastContainer />        @* notifications (IToastService) *@
        <UiMessageBoxProvider />    @* confirmations et alertes (IMessageBoxService) *@
        <UiDialogProvider />        @* dialogues avec composant (IUiDialogService) *@
        <UiPopoverProvider />       @* popovers et listes déroulantes jamais rognés *@
        <UiThemeProvider />         @* mode clair / sombre, densité, thème *@
        """;

    public const string FirstComponent = """
        <UiButton Text="Bonjour" Color="UiColor.Success" OnClick="SayHello" />

        @code {
            private void SayHello() { /* ... */ }
        }
        """;

    public const string ThemeDefinition = """
        private readonly UiThemeDefinition _theme = new()
        {
            Light = { Primary = "#0d9488", Success = "#16a34a", Background = "#f8fafc" },
            Dark  = { Primary = "#2dd4bf", Background = "#020617", Surface = "#0f172a" },
            Typography = { FontFamily = "'Inter', system-ui, sans-serif" },
            Layout = { BorderRadius = 14 }   // rayon de référence : les autres en sont déduits
        };
        """;

    public const string ThemeApply = """
        <UiThemeProvider Theme="UiTheme.System" Definition="_theme" />
        """;

    public const string ThemeAccessible = """
        @* Préréglage conforme WCAG AA, personnalisable comme n'importe quel thème. *@
        <UiThemeProvider Theme="UiTheme.System" Definition="_theme" />

        @code {
            private readonly UiThemeDefinition _theme = UiThemeDefinition.Accessible();
        }
        """;

    public const string NavMenuUsage = """
        <UiNavMenu BrandText="Mon application" BrandHref="" NavigationItems="_items" AriaLabel="Navigation principale" />

        @code {
            private readonly IReadOnlyList<NavigationItem> _items = new[]
            {
                new NavigationItem { Text = "Accueil", Href = "", Match = NavLinkMatch.All, Group = "Général" },
                new NavigationItem { Text = "Clients", Href = "clients", Group = "Général" },
                new NavigationItem { Text = "Facturation", Href = "facturation", Group = "Gestion" },
                new NavigationItem { Text = "Archives", Href = "archives", Group = "Gestion", Disabled = true }
            };
        }
        """;

    public const string LayoutUsage = """
        @* Layouts/MainLayout.razor *@
        @inherits LayoutComponentBase

        <UiLayout BrandText="Mon application" VersionText="v1.4" ShowFooter="false" MainBody="@Body">
            <NavMenu>
                <UiNavMenu BrandText="Menu" NavigationItems="_items" />
            </NavMenu>
        </UiLayout>

        <UiToastContainer />
        <UiThemeProvider Theme="UiTheme.System" Density="UiDensity.Comfortable" />
        """;

    public const string SimpleLayoutUsage = """
        @* Layout sans menu latéral : pages de connexion, écrans plein cadre. *@
        @inherits LayoutComponentBase

        <UiSimpleLayout BrandText="Mon application" MainBody="@Body" />
        """;

    public const string CssVariables = """
        /* Surcharge ponctuelle, sans écrire de thème complet */
        :root {
            --primary: #0d9488;
            --radius-md: 14px;
        }
        """;

    public const string DataAnnotationsModel = """
        public sealed class Signup
        {
            [Required(ErrorMessage = "Le nom est obligatoire.")]
            public string? Name { get; set; }

            [Range(18, 120, ErrorMessage = "L'âge doit être compris entre 18 et 120 ans.")]
            public int Age { get; set; } = 30;
        }
        """;

    public const string FormMethods = """
        <UiForm @ref="_form" Model="_model" OnValidSubmit="Save">...</UiForm>

        @code {
            private UiForm? _form;

            private async Task Check()
            {
                if (_form!.Validate())   // valide tous les champs, DataAnnotations et Validation comprises
                {
                    await Save();
                }
            }

            private void Clear() => _form!.ResetValidation();   // efface les erreurs, garde les valeurs
        }
        """;

    public const string GridServerSignature = """
        private Task<UiGridData<Order>> LoadAsync(UiGridState state, CancellationToken cancellationToken)
        {
            // state.Page, state.PageSize, state.Search, state.Sorts (Key + Descending), state.Filters (Key + Operator + Value)
            // Renvoyez les lignes de la page et le total, tous filtres appliqués :
            return Api.SearchOrdersAsync(state, cancellationToken);
        }
        """;
}
