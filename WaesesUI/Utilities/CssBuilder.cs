using System.Text;

namespace WaesesUI;

/// <summary>Assemble une liste de classes CSS en ignorant les valeurs vides.</summary>
public readonly struct CssBuilder
{
    private readonly string _classes;

    private CssBuilder(string classes) => _classes = classes;

    public static CssBuilder Default(string? baseClass = null) => new(baseClass?.Trim() ?? string.Empty);

    public CssBuilder Add(string? value) =>
        string.IsNullOrWhiteSpace(value) ? this : new(_classes.Length == 0 ? value.Trim() : $"{_classes} {value.Trim()}");

    public CssBuilder AddIf(string? value, bool condition) => condition ? Add(value) : this;

    public CssBuilder AddIf(string? value, Func<bool> condition) => AddIf(value, condition());

    public string Build() => _classes;

    public override string ToString() => _classes;
}

/// <summary>Assemble des déclarations de style inline (<c>prop: valeur;</c>).</summary>
public readonly struct StyleBuilder
{
    private readonly string _styles;

    private StyleBuilder(string styles) => _styles = styles;

    public static StyleBuilder Default(string? baseStyle = null) => new(Normalize(baseStyle));

    public StyleBuilder Add(string? declaration) =>
        string.IsNullOrWhiteSpace(declaration) ? this : new(_styles + Normalize(declaration));

    public StyleBuilder AddIf(string? declaration, bool condition) => condition ? Add(declaration) : this;

    public string? Build() => _styles.Length == 0 ? null : _styles.TrimEnd();

    public override string ToString() => _styles;

    private static string Normalize(string? declaration)
    {
        if (string.IsNullOrWhiteSpace(declaration)) return string.Empty;
        var text = declaration.Trim();
        return text.EndsWith(';') ? text + " " : text + "; ";
    }
}
