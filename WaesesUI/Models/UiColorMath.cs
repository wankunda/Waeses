using System.Globalization;

namespace WaesesUI;

/// <summary>Calculs sur couleurs hexadécimales (<c>#rgb</c> ou <c>#rrggbb</c>) pour dériver survol, teinte claire et contraste.</summary>
public static class UiColorMath
{
    public static bool TryParseHex(string? value, out (int R, int G, int B) rgb)
    {
        rgb = default;
        if (string.IsNullOrEmpty(value) || value[0] != '#') return false;

        var hex = value[1..];
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => new string(c, 2)));
        }
        if (hex.Length != 6 || !hex.All(Uri.IsHexDigit)) return false;

        rgb = (
            int.Parse(hex.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            int.Parse(hex.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
        return true;
    }

    public static string ToHex((int R, int G, int B) rgb) =>
        $"#{Math.Clamp(rgb.R, 0, 255):x2}{Math.Clamp(rgb.G, 0, 255):x2}{Math.Clamp(rgb.B, 0, 255):x2}";

    /// <summary>Mélange <paramref name="color"/> avec <paramref name="target"/> ; <paramref name="amount"/> de 0 (inchangé) à 1 (cible).</summary>
    public static string Mix(string color, string target, double amount)
    {
        if (!TryParseHex(color, out var a) || !TryParseHex(target, out var b)) return color;
        amount = Math.Clamp(amount, 0, 1);
        return ToHex((
            (int)Math.Round(a.R + (b.R - a.R) * amount),
            (int)Math.Round(a.G + (b.G - a.G) * amount),
            (int)Math.Round(a.B + (b.B - a.B) * amount)));
    }

    public static string Darken(string color, double amount) => Mix(color, "#000000", amount);

    public static string Lighten(string color, double amount) => Mix(color, "#ffffff", amount);

    /// <summary>Couleur avec transparence (<c>rgba(...)</c>) ; renvoie <paramref name="color"/> si ce n'est pas un hexadécimal.</summary>
    public static string WithAlpha(string color, double alpha) =>
        TryParseHex(color, out var rgb)
            ? string.Create(CultureInfo.InvariantCulture, $"rgba({rgb.R}, {rgb.G}, {rgb.B}, {Math.Clamp(alpha, 0, 1):0.##})")
            : color;

    /// <summary>Luminance relative (WCAG), de 0 (noir) à 1 (blanc).</summary>
    public static double Luminance(string color)
    {
        if (!TryParseHex(color, out var rgb)) return 0;

        static double Channel(int value)
        {
            var c = value / 255d;
            return c <= .04045 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4);
        }

        return .2126 * Channel(rgb.R) + .7152 * Channel(rgb.G) + .0722 * Channel(rgb.B);
    }

    /// <summary>Rapport de contraste WCAG entre deux couleurs hexadécimales, de 1 (identiques) à 21 (noir sur blanc).</summary>
    public static double ContrastRatio(string first, string second)
    {
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }

    /// <summary>Texte le plus lisible sur <paramref name="color"/> : blanc ou bleu nuit (<c>#0f172a</c>), selon le meilleur rapport de contraste.</summary>
    public static string Contrast(string color)
    {
        if (!TryParseHex(color, out _)) return "#ffffff";
        return ContrastRatio(color, "#ffffff") >= ContrastRatio(color, "#0f172a") ? "#ffffff" : "#0f172a";
    }
}
