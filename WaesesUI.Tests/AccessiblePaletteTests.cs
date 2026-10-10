namespace WaesesUI.Tests;

/// <summary>
/// <c>UiThemeDefinition.Accessible()</c> : chaque couleur doit atteindre 4,5:1 (WCAG AA, texte normal) dans tous les rôles où le thème l'emploie.
/// Le calcul est celui que fait le thème lui-même pour dériver survol, teinte claire et couleur de texte.
/// </summary>
public sealed class AccessiblePaletteTests
{
    private const double Aa = 4.5;
    private const string LightSurface = "#ffffff";
    private const string LightSecondarySurface = "#f1f5f9";
    private const string DarkSurface = "#111827";
    private const string DarkSecondarySurface = "#1f293d";

    private static UiThemeDefinition Theme => UiThemeDefinition.Accessible();

    private static IEnumerable<(string Name, string Color)> Brand(UiPaletteDefinition palette) => new[]
    {
        ("primary", palette.Primary!), ("secondary", palette.Secondary!), ("success", palette.Success!),
        ("danger", palette.Danger!), ("warning", palette.Warning!), ("info", palette.Info!)
    };

    public static IEnumerable<object[]> LightColors() => Brand(Theme.Light).Select(c => new object[] { c.Name, c.Color });
    public static IEnumerable<object[]> DarkColors() => Brand(Theme.Dark).Select(c => new object[] { c.Name, c.Color });

    [Theory]
    [MemberData(nameof(LightColors))]
    public void Mode_clair_la_couleur_est_lisible_en_texte_sur_la_surface_et_sur_sa_teinte_claire(string name, string color)
    {
        var tint = UiColorMath.Lighten(color, .92);                       // --x-light, comme dérivée par le thème

        Assert.True(UiColorMath.ContrastRatio(color, LightSurface) >= Aa, $"{name} {color} sur blanc : {UiColorMath.ContrastRatio(color, LightSurface):0.00}");
        Assert.True(UiColorMath.ContrastRatio(color, tint) >= Aa, $"{name} {color} sur sa teinte {tint} : {UiColorMath.ContrastRatio(color, tint):0.00}");
    }

    [Theory]
    [MemberData(nameof(LightColors))]
    public void Mode_clair_le_texte_des_boutons_pleins_est_lisible_au_repos_et_au_survol(string name, string color)
    {
        var text = UiColorMath.Contrast(color);                           // --x-contrast
        var hover = UiColorMath.Darken(color, .10);                       // --x-hover

        Assert.True(UiColorMath.ContrastRatio(color, text) >= Aa, $"{name} : {UiColorMath.ContrastRatio(color, text):0.00} au repos");
        Assert.True(UiColorMath.ContrastRatio(hover, text) >= Aa, $"{name} : {UiColorMath.ContrastRatio(hover, text):0.00} au survol");
    }

    [Theory]
    [MemberData(nameof(DarkColors))]
    public void Mode_sombre_la_couleur_est_lisible_en_texte_sur_les_deux_surfaces(string name, string color)
    {
        Assert.True(UiColorMath.ContrastRatio(color, DarkSurface) >= Aa, $"{name} {color} sur la surface : {UiColorMath.ContrastRatio(color, DarkSurface):0.00}");
        Assert.True(UiColorMath.ContrastRatio(color, DarkSecondarySurface) >= Aa, $"{name} {color} sur la surface secondaire : {UiColorMath.ContrastRatio(color, DarkSecondarySurface):0.00}");
    }

    [Theory]
    [MemberData(nameof(DarkColors))]
    public void Mode_sombre_le_texte_des_boutons_pleins_est_lisible_au_repos_et_au_survol(string name, string color)
    {
        var text = UiColorMath.Contrast(color);
        var hover = UiColorMath.Lighten(color, .15);

        Assert.True(UiColorMath.ContrastRatio(color, text) >= Aa, $"{name} : {UiColorMath.ContrastRatio(color, text):0.00} au repos");
        Assert.True(UiColorMath.ContrastRatio(hover, text) >= Aa, $"{name} : {UiColorMath.ContrastRatio(hover, text):0.00} au survol");
    }

    [Fact]
    public void Le_texte_attenue_reste_lisible_sur_toutes_les_surfaces_de_chaque_mode()
    {
        foreach (var surface in new[] { LightSurface, LightSecondarySurface, "#f8fafc" })
        {
            Assert.True(UiColorMath.ContrastRatio(Theme.Light.TextMuted!, surface) >= Aa, $"clair, muted sur {surface}");
        }
        foreach (var surface in new[] { DarkSurface, DarkSecondarySurface, "#090d16" })
        {
            Assert.True(UiColorMath.ContrastRatio(Theme.Dark.TextMuted!, surface) >= Aa, $"sombre, muted sur {surface}");
        }
    }

    [Fact]
    public void Le_theme_par_defaut_ne_respecte_pas_AA_pour_ses_couleurs_semantiques_en_texte_clair()
    {
        // Documente pourquoi le préréglage existe : ces valeurs sont celles de theme.css (mode clair).
        foreach (var color in new[] { "#10b981", "#f59e0b", "#06b6d4", "#ef4444" })
        {
            Assert.True(UiColorMath.ContrastRatio(color, LightSurface) < Aa, $"{color} atteint déjà {UiColorMath.ContrastRatio(color, LightSurface):0.00}");
        }
    }

    [Fact]
    public void Le_preset_produit_un_css_valide_avec_les_variables_attendues()
    {
        var css = Theme.ToCss();

        Assert.Contains("--success:#0b7c56", css);
        Assert.Contains("--text-muted:#606f85", css);
        Assert.Contains("[data-theme=\"dark\"]{", css);
        Assert.Contains("--text-muted:#8b9ab3", css);
        Assert.NotSame(UiThemeDefinition.Accessible(), UiThemeDefinition.Accessible());   // chaque appel renvoie un objet modifiable indépendant
    }

    [Fact]
    public void Le_preset_se_personnalise_sans_modifier_les_autres_instances()
    {
        var custom = UiThemeDefinition.Accessible();
        custom.Light.Primary = "#0d9488";

        Assert.Contains("--primary:#0d9488", custom.ToCss());
        Assert.Contains("--primary:#5144e4", UiThemeDefinition.Accessible().ToCss());
    }
}
