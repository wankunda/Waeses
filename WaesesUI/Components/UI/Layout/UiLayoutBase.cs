using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>
/// Base commune des layouts (<see cref="UiLayout"/>, <see cref="UiSimpleLayout"/>, <see cref="UiTabLayout"/>) :
/// nom de l'application, bouton clair/sombre, contenu de la page et pied de page.
/// Elle gère aussi le mode clair/sombre avec <see cref="IThemeService"/> : chaque layout n'a plus qu'à dessiner sa structure.
/// </summary>
public abstract class UiLayoutBase : UiComponentBase, IDisposable
{
    /// <summary>Service du thème, injecté par Blazor.</summary>
    [Inject] protected IThemeService ThemeService { get; set; } = default!;

    /// <summary>Nom de l'application, affiché dans l'en-tête.</summary>
    [Parameter] public string BrandText { get; set; } = "Waeses UI";

    /// <summary>Icône ou emoji devant le nom.</summary>
    [Parameter] public string? BrandIcon { get; set; } = "⚡";

    /// <summary>Affiche le bouton clair/sombre dans l'en-tête.</summary>
    [Parameter] public bool ShowThemeToggle { get; set; } = true;

    /// <summary>Affiche le libellé (« Mode Sombre », « Mode Clair ») à côté de l'emoji du bouton clair/sombre.</summary>
    [Parameter] public bool IsShowTextMode { get; set; }

    /// <summary>Indique si le mode sombre est actif.</summary>
    [Parameter] public bool IsDarkMode { get; set; }

    /// <summary>Appelé quand l'utilisateur change de mode.</summary>
    [Parameter] public EventCallback<bool> IsDarkModeChanged { get; set; }

    /// <summary>Contenu de la page (le <c>Body</c> du layout Blazor).</summary>
    [Parameter] public RenderFragment? MainBody { get; set; }

    /// <summary>Contenu supplémentaire du pied de page.</summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>Affiche le pied de page (texte et fragment <see cref="Footer"/>). Vrai par défaut, sauf dans <see cref="UiTabLayout"/>.</summary>
    [Parameter] public bool ShowFooter { get; set; } = true;

    /// <summary>Texte du pied de page. Null : « © année Waeses. All rights reserved. » ; chaîne vide : aucun texte.</summary>
    [Parameter] public string? FooterText { get; set; }

    /// <summary>Libellé du bouton clair/sombre : il annonce le mode vers lequel on bascule.</summary>
    protected string ModeText => IsDarkMode ? "Mode Clair" : "Mode Sombre";

    /// <summary>Emoji du bouton clair/sombre.</summary>
    protected string ModeIcon => IsDarkMode ? "☀️" : "🌙";

    /// <summary>Texte affiché dans le pied de page (celui de <see cref="FooterText"/>, ou le texte par défaut).</summary>
    protected string FooterLabel => FooterText ?? $"© {DateTime.Now.Year} Waeses. All rights reserved.";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Le thème se lit en JavaScript (localStorage, préférence du système) : seulement après le premier rendu.
            await ThemeService.InitializeThemeAsync();
            IsDarkMode = ThemeService.IsDarkMode;
            await IsDarkModeChanged.InvokeAsync(IsDarkMode);
            ThemeService.OnThemeChanged += HandleThemeChanged;
            StateHasChanged();
        }
    }

    /// <summary>Bascule entre clair et sombre (clic sur le bouton de l'en-tête).</summary>
    protected async Task ToggleTheme()
    {
        await ThemeService.ToggleThemeAsync();
        IsDarkMode = ThemeService.IsDarkMode;
        await IsDarkModeChanged.InvokeAsync(IsDarkMode);
    }

    // Le thème peut changer ailleurs (UiThemeProvider, autre composant) : le layout suit.
    private async void HandleThemeChanged()
    {
        IsDarkMode = ThemeService.IsDarkMode;
        await IsDarkModeChanged.InvokeAsync(IsDarkMode);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Se désabonne du service du thème quand le layout disparaît.</summary>
    public virtual void Dispose()
    {
        ThemeService.OnThemeChanged -= HandleThemeChanged;
        GC.SuppressFinalize(this);
    }
}
