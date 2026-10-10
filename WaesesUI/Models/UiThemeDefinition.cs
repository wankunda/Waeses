using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WaesesUI;

/// <summary>
/// Thème décrit en C# (équivalent de MudTheme) : palettes claire et sombre, typographie et formes.
/// Seules les valeurs renseignées sont générées ; le reste vient de <c>theme.css</c>.
/// </summary>
public sealed class UiThemeDefinition
{
    public UiPaletteDefinition Light { get; set; } = new();
    public UiPaletteDefinition Dark { get; set; } = new();
    public UiTypographyDefinition Typography { get; set; } = new();
    public UiLayoutDefinition Layout { get; set; } = new();

    /// <summary>
    /// Palette dont chaque couleur respecte WCAG 2.1 AA (rapport de contraste d'au moins 4,5:1) : en texte sur la surface
    /// (blanche en mode clair, <c>#111827</c> et <c>#1f293d</c> en mode sombre), sur sa propre teinte claire, et avec le texte
    /// choisi par <see cref="UiColorMath.Contrast"/> quand elle sert de fond (boutons pleins). Les teintes sont celles du thème
    /// par défaut, assombries (mode clair) ou éclaircies (mode sombre) juste assez.
    /// </summary>
    /// <remarks>
    /// Les couleurs par défaut du thème (vert, ambre et cyan surtout) ne passent pas ce seuil en texte sur fond clair :
    /// ce préréglage est donc à utiliser quand l'accessibilité des contrastes compte. Il se personnalise comme tout thème.
    /// </remarks>
    public static UiThemeDefinition Accessible() => new()
    {
        Light =
        {
            Primary = "#5144e4", Secondary = "#606f85", Success = "#0b7c56", Danger = "#d71212", Warning = "#966106", Info = "#04788c",
            TextMuted = "#606f85"
        },
        Dark =
        {
            Primary = "#8e85f1", Secondary = "#94a3b8", Success = "#34d399", Danger = "#f87171", Warning = "#fbbf24", Info = "#38bdf8",
            TextMuted = "#8b9ab3"
        }
    };

    /// <summary>
    /// Feuille CSS du thème : variables du mode clair (<c>:root</c> hors mode sombre), du mode sombre
    /// (<c>[data-theme="dark"]</c>), typographie et rayons. Lève <see cref="InvalidOperationException"/> pour une valeur invalide.
    /// </summary>
    public string ToCss()
    {
        var light = new List<string>();
        light.AddRange(Light.ToDeclarations(isDark: false));
        light.AddRange(Typography.ToDeclarations());
        light.AddRange(Layout.ToDeclarations());

        var dark = Dark.ToDeclarations(isDark: true).ToList();

        var css = new StringBuilder();
        if (light.Count > 0)
        {
            // Plus spécifique que le :root de theme.css, sans écraser les valeurs du mode sombre.
            css.Append(":root:not([data-theme=\"dark\"]){").Append(string.Join(';', light)).Append("}\n");
        }
        if (dark.Count > 0)
        {
            css.Append("[data-theme=\"dark\"]{").Append(string.Join(';', dark)).Append('}');
        }
        return css.ToString();
    }

    internal static string Color(string property, string? value)
    {
        if (value is null) throw new InvalidOperationException($"Couleur manquante pour {property}.");
        if (!UiCssValue.IsColor(value)) throw new InvalidOperationException($"Valeur de couleur invalide pour {property} : « {value} ».");
        return value.Trim();
    }
}

/// <summary>Couleurs d'un mode (clair ou sombre). Les teintes dérivées (survol, clair, contraste) sont calculées à partir de la couleur de base hexadécimale.</summary>
public sealed class UiPaletteDefinition
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Success { get; set; }
    public string? Danger { get; set; }
    public string? Warning { get; set; }
    public string? Info { get; set; }

    /// <summary>Fond de l'application.</summary>
    public string? Background { get; set; }
    public string? Surface { get; set; }
    public string? SurfaceElevated { get; set; }
    public string? SurfaceSecondary { get; set; }
    public string? SurfaceTertiary { get; set; }
    public string? TextPrimary { get; set; }
    public string? TextSecondary { get; set; }
    public string? TextMuted { get; set; }
    public string? Border { get; set; }
    public string? BorderStrong { get; set; }

    internal IEnumerable<string> ToDeclarations(bool isDark)
    {
        foreach (var (name, value) in new[] { ("primary", Primary), ("secondary", Secondary), ("success", Success), ("danger", Danger), ("warning", Warning), ("info", Info) })
        {
            if (value is null) continue;
            foreach (var declaration in BrandDeclarations(name, UiThemeDefinition.Color($"--{name}", value), isDark))
            {
                yield return declaration;
            }
        }

        foreach (var (name, value) in new[]
        {
            ("bg-app", Background), ("bg-surface", Surface), ("bg-surface-elevated", SurfaceElevated),
            ("bg-surface-secondary", SurfaceSecondary), ("bg-surface-tertiary", SurfaceTertiary),
            ("text-main", TextPrimary), ("text-secondary", TextSecondary), ("text-muted", TextMuted),
            ("border-default", Border), ("border-strong", BorderStrong)
        })
        {
            if (value is not null) yield return $"--{name}:{UiThemeDefinition.Color($"--{name}", value)}";
        }
    }

    private static IEnumerable<string> BrandDeclarations(string name, string color, bool isDark)
    {
        yield return $"--{name}:{color}";

        // Les teintes dérivées ne se calculent que depuis un hexadécimal ; sinon on garde les valeurs de theme.css.
        if (!UiColorMath.TryParseHex(color, out _)) yield break;

        yield return $"--{name}-hover:{(isDark ? UiColorMath.Lighten(color, .15) : UiColorMath.Darken(color, .10))}";
        yield return $"--{name}-light:{(isDark ? UiColorMath.WithAlpha(color, .16) : UiColorMath.Lighten(color, .92))}";
        yield return $"--{name}-contrast:{UiColorMath.Contrast(color)}";

        if (name == "primary")
        {
            yield return $"--primary-active:{(isDark ? UiColorMath.Darken(color, .08) : UiColorMath.Darken(color, .20))}";
            yield return $"--primary-subtle:{(isDark ? UiColorMath.WithAlpha(color, .25) : UiColorMath.Lighten(color, .82))}";
        }
    }
}

public sealed class UiTypographyDefinition
{
    /// <summary>Famille de police du texte (par exemple <c>"Inter", system-ui, sans-serif</c>).</summary>
    public string? FontFamily { get; set; }

    /// <summary>Famille de police à chasse fixe (code).</summary>
    public string? MonoFontFamily { get; set; }

    internal IEnumerable<string> ToDeclarations()
    {
        if (FontFamily is not null) yield return $"--font-sans:{UiCssValue.FontFamily(FontFamily)}";
        if (MonoFontFamily is not null) yield return $"--font-mono:{UiCssValue.FontFamily(MonoFontFamily)}";
    }
}

public sealed class UiLayoutDefinition
{
    /// <summary>Rayon de référence en pixels (rayon des champs et boutons, 10 par défaut) ; les autres rayons en sont déduits.</summary>
    public int? BorderRadius { get; set; }

    internal IEnumerable<string> ToDeclarations()
    {
        if (BorderRadius is not { } radius) yield break;
        if (radius < 0 || radius > 64) throw new InvalidOperationException($"BorderRadius doit être compris entre 0 et 64 (valeur : {radius}).");

        string Px(double factor) => string.Create(CultureInfo.InvariantCulture, $"{Math.Round(radius * factor)}px");
        yield return $"--radius-xs:{Px(.4)}";
        yield return $"--radius-sm:{Px(.6)}";
        yield return $"--radius-md:{Px(1)}";
        yield return $"--radius-lg:{Px(1.4)}";
        yield return $"--radius-xl:{Px(2)}";
    }
}

internal static partial class UiCssValue
{
    // Liste blanche : hexadécimal, rgb()/hsl() simples, var(--x), mot-clé de couleur. Rien qui permette de sortir de la déclaration.
    [GeneratedRegex(@"^(#[0-9a-fA-F]{3,8}|(rgb|hsl)a?\([0-9.,%\s/]+\)|var\(--[a-zA-Z0-9-]+\)|[a-zA-Z]+)$")]
    private static partial Regex ColorPattern();

    [GeneratedRegex(@"^[A-Za-z0-9 ,'""_.\-]+$")]
    private static partial Regex FontPattern();

    public static bool IsColor(string value) => ColorPattern().IsMatch(value.Trim());

    public static string FontFamily(string value)
    {
        if (!FontPattern().IsMatch(value)) throw new InvalidOperationException($"Famille de police invalide : « {value} ».");
        return value.Trim();
    }
}
