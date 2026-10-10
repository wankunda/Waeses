using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class ThemeDefinitionTests : TestContext
{
    public ThemeDefinitionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // ---------- UiColorMath ----------
    [Theory]
    [InlineData("#fff", true, 255, 255, 255)]
    [InlineData("#0f172a", true, 15, 23, 42)]
    [InlineData("#6366F1", true, 99, 102, 241)]
    [InlineData("6366f1", false, 0, 0, 0)]
    [InlineData("#12345", false, 0, 0, 0)]
    [InlineData("#gggggg", false, 0, 0, 0)]
    [InlineData(null, false, 0, 0, 0)]
    public void TryParseHex(string? value, bool ok, int r, int g, int b)
    {
        Assert.Equal(ok, UiColorMath.TryParseHex(value, out var rgb));
        Assert.Equal((r, g, b), rgb);
    }

    [Fact]
    public void Mix_Darken_Lighten_et_WithAlpha()
    {
        Assert.Equal("#808080", UiColorMath.Mix("#000000", "#ffffff", .5));
        Assert.Equal("#000000", UiColorMath.Darken("#ffffff", 1));
        Assert.Equal("#ffffff", UiColorMath.Lighten("#000000", 1));
        Assert.Equal("#336699", UiColorMath.Mix("#336699", "#ffffff", 0));
        Assert.Equal("rgba(255, 0, 0, 0.16)", UiColorMath.WithAlpha("#ff0000", .16));
        Assert.Equal("red", UiColorMath.WithAlpha("red", .5));          // non hexadécimal : inchangé
        Assert.Equal("blue", UiColorMath.Darken("blue", .2));
    }

    [Fact]
    public void Contrast_choisit_un_texte_lisible()
    {
        Assert.Equal("#ffffff", UiColorMath.Contrast("#1e293b"));
        Assert.Equal("#0f172a", UiColorMath.Contrast("#fde68a"));
        Assert.Equal("#ffffff", UiColorMath.Contrast("red"));            // inconnu : blanc
        Assert.Equal("#0f172a", UiColorMath.Contrast("#10b981"));        // vert : le blanc ne ferait que 2,5:1
        Assert.Equal("#0f172a", UiColorMath.Contrast("#f59e0b"));        // ambre
        Assert.Equal("#ffffff", UiColorMath.Contrast("#4f46e5"));        // indigo foncé
        Assert.True(UiColorMath.Luminance("#ffffff") > UiColorMath.Luminance("#000000"));
    }

    [Theory]
    [InlineData("#10b981")] [InlineData("#f59e0b")] [InlineData("#06b6d4")] [InlineData("#ef4444")] [InlineData("#6366f1")]
    [InlineData("#0d9488")] [InlineData("#64748b")] [InlineData("#ffffff")] [InlineData("#000000")] [InlineData("#808080")]
    public void Contrast_choisit_toujours_le_meilleur_des_deux_textes(string background)
    {
        var chosen = UiColorMath.Contrast(background);
        var other = chosen == "#ffffff" ? "#0f172a" : "#ffffff";

        Assert.True(UiColorMath.ContrastRatio(background, chosen) >= UiColorMath.ContrastRatio(background, other));
    }

    [Fact]
    public void ContrastRatio_vaut_21_pour_noir_sur_blanc_et_1_pour_deux_couleurs_identiques()
    {
        Assert.Equal(21, UiColorMath.ContrastRatio("#000000", "#ffffff"), 1);
        Assert.Equal(1, UiColorMath.ContrastRatio("#336699", "#336699"), 3);
        Assert.Equal(UiColorMath.ContrastRatio("#123456", "#ffffff"), UiColorMath.ContrastRatio("#ffffff", "#123456"), 6);
    }

    // ---------- ToCss ----------
    [Fact]
    public void Un_theme_vide_ne_genere_aucun_css()
    {
        Assert.Equal(string.Empty, new UiThemeDefinition().ToCss());
    }

    [Fact]
    public void La_couleur_primaire_claire_derive_survol_teinte_et_contraste()
    {
        var css = new UiThemeDefinition { Light = { Primary = "#0d9488" } }.ToCss();

        Assert.StartsWith(":root:not([data-theme=\"dark\"]){", css);
        Assert.Contains("--primary:#0d9488", css);
        Assert.Contains($"--primary-hover:{UiColorMath.Darken("#0d9488", .10)}", css);
        Assert.Contains($"--primary-light:{UiColorMath.Lighten("#0d9488", .92)}", css);
        Assert.Contains("--primary-active:", css);
        Assert.Contains("--primary-subtle:", css);
        Assert.Contains($"--primary-contrast:{UiColorMath.Contrast("#0d9488")}", css);
        Assert.DoesNotContain("[data-theme=\"dark\"]{", css);
    }

    [Fact]
    public void Le_mode_sombre_utilise_des_teintes_eclaircies_et_translucides()
    {
        var css = new UiThemeDefinition { Dark = { Primary = "#2dd4bf", Background = "#020617" } }.ToCss();

        Assert.StartsWith("[data-theme=\"dark\"]{", css);
        Assert.Contains($"--primary-hover:{UiColorMath.Lighten("#2dd4bf", .15)}", css);
        Assert.Contains("--primary-light:rgba(45, 212, 191, 0.16)", css);
        Assert.Contains("--bg-app:#020617", css);
        Assert.DoesNotContain(":root", css);
    }

    [Fact]
    public void Les_autres_couleurs_de_marque_ont_survol_clair_et_contraste_sans_active()
    {
        var css = new UiThemeDefinition { Light = { Success = "#16a34a" } }.ToCss();

        Assert.Contains("--success:#16a34a", css);
        Assert.Contains("--success-hover:", css);
        Assert.Contains("--success-light:", css);
        Assert.Contains("--success-contrast:", css);
        Assert.DoesNotContain("--success-active", css);
    }

    [Fact]
    public void Une_couleur_non_hexadecimale_n_emet_que_la_variable_de_base()
    {
        var css = new UiThemeDefinition { Light = { Primary = "rgb(10, 20, 30)", Surface = "var(--mon-fond)" } }.ToCss();

        Assert.Contains("--primary:rgb(10, 20, 30)", css);
        Assert.DoesNotContain("--primary-hover", css);
        Assert.Contains("--bg-surface:var(--mon-fond)", css);
    }

    [Fact]
    public void Typographie_et_rayons()
    {
        var css = new UiThemeDefinition
        {
            Typography = { FontFamily = "'Inter', system-ui, sans-serif", MonoFontFamily = "Fira Code, monospace" },
            Layout = { BorderRadius = 20 }
        }.ToCss();

        Assert.Contains("--font-sans:'Inter', system-ui, sans-serif", css);
        Assert.Contains("--font-mono:Fira Code, monospace", css);
        Assert.Contains("--radius-md:20px", css);
        Assert.Contains("--radius-sm:12px", css);
        Assert.Contains("--radius-xl:40px", css);
    }

    [Theory]
    [InlineData("red; } body { display:none")]
    [InlineData("#fff</style><script>alert(1)</script>")]
    [InlineData("url(javascript:alert(1))")]
    [InlineData("expression(alert(1))")]
    public void Une_couleur_dangereuse_est_refusee(string value)
    {
        var theme = new UiThemeDefinition { Light = { Primary = value } };
        Assert.Throws<InvalidOperationException>(() => theme.ToCss());
    }

    [Theory]
    [InlineData("Inter; } body { color:red")]
    [InlineData("a</style>")]
    [InlineData("a{b}")]
    public void Une_police_dangereuse_est_refusee(string value)
    {
        var theme = new UiThemeDefinition { Typography = { FontFamily = value } };
        Assert.Throws<InvalidOperationException>(() => theme.ToCss());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65)]
    public void Un_rayon_hors_limites_est_refuse(int radius)
    {
        var theme = new UiThemeDefinition { Layout = { BorderRadius = radius } };
        Assert.Throws<InvalidOperationException>(() => theme.ToCss());
    }

    // ---------- UiThemeProvider ----------
    [Fact]
    public void Le_provider_rend_la_feuille_de_style_de_la_definition()
    {
        Services.AddWaesesUIServices();
        var cut = RenderComponent<UiThemeProvider>(p => p.Add(c => c.Definition, new UiThemeDefinition { Light = { Primary = "#0d9488" } }));

        Assert.Contains("--primary:#0d9488", cut.Find("style[data-ui-theme]").InnerHtml);
    }

    [Fact]
    public void Le_provider_utilise_le_theme_des_options_quand_aucune_definition_n_est_passee()
    {
        Services.AddWaesesUIServices(o => o.Theme = new UiThemeDefinition { Dark = { Primary = "#2dd4bf" } });
        var cut = RenderComponent<UiThemeProvider>();

        Assert.Contains("--primary:#2dd4bf", cut.Find("style[data-ui-theme]").InnerHtml);
    }

    [Fact]
    public void Sans_theme_le_provider_ne_rend_aucun_style_et_garde_les_palettes_historiques()
    {
        Services.AddWaesesUIServices();
        var cut = RenderComponent<UiThemeProvider>();

        Assert.Empty(cut.FindAll("style"));
        cut.WaitForAssertion(() =>
        {
            var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.applyGlobalTheme");
            var palette = Assert.IsType<Dictionary<string, string>>(call.Arguments[2]);
            Assert.NotEmpty(palette);
        });
    }

    [Fact]
    public void Avec_une_definition_les_palettes_en_ligne_ne_sont_pas_envoyees()
    {
        Services.AddWaesesUIServices();
        RenderComponent<UiThemeProvider>(p => p.Add(c => c.Definition, new UiThemeDefinition { Light = { Primary = "#0d9488" } }));

        var call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.applyGlobalTheme");
        Assert.Empty(Assert.IsType<Dictionary<string, string>>(call.Arguments[2]));
    }

    [Fact]
    public void Une_valeur_invalide_dans_le_theme_echoue_au_rendu_avec_un_message_clair()
    {
        Services.AddWaesesUIServices();
        var ex = Assert.ThrowsAny<Exception>(() => RenderComponent<UiThemeProvider>(p => p
            .Add(c => c.Definition, new UiThemeDefinition { Light = { Primary = "red; x" } })));

        Assert.Contains("--primary", ex.ToString());
    }
}
