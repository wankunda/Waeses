using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using WaesesUI;

namespace WaesesUIDocs.Docs;

public enum ApiParameterKind
{
    Parameter,
    Event,
    Content
}

/// <summary>Un paramètre public d'un composant, tel que présenté dans le tableau d'API.</summary>
public sealed record ApiParameter(
    string Name,
    string TypeName,
    string? Default,
    string Summary,
    bool Required,
    ApiParameterKind Kind,
    Type DeclaringType);

/// <summary>
/// Documentation d'API d'un composant, générée : paramètres par réflexion (<c>[Parameter]</c>), descriptions lues dans
/// le fichier XML de la bibliothèque (<c>///</c> du code source). Rien n'est à maintenir à la main.
/// </summary>
public static partial class ApiDocs
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Members = new(LoadMembers);

    // Paramètres hérités du framework (InputBase) qui n'ont pas de commentaire XML dans WaesesUI.xml.
    private static readonly Dictionary<string, string> FrameworkSummaries = new()
    {
        ["Value"] = "Valeur liée au champ (utilisez `@bind-Value`).",
        ["ValueChanged"] = "Appelé lorsque la valeur change (fourni par `@bind-Value`).",
        ["ValueExpression"] = "Expression désignant la valeur liée, utilisée par la validation (fournie par `@bind-Value`).",
        ["DisplayName"] = "Nom du champ utilisé dans les messages de validation générés.",
        ["AdditionalAttributes"] = "Attributs HTML supplémentaires transmis à l'élément (`id`, `data-*`, `aria-*`…)."
    };

    /// <summary>Description (<c>summary</c>) d'un membre ou d'un type, ou <c>null</c> s'il n'est pas documenté.</summary>
    public static string? Summary(MemberInfo member)
    {
        var declaring = member.DeclaringType is { IsGenericType: true } generic ? generic.GetGenericTypeDefinition() : member.DeclaringType;
        var key = member is Type type
            ? $"T:{(type.IsGenericType ? type.GetGenericTypeDefinition() : type).FullName}"
            : $"{(member is PropertyInfo ? 'P' : 'F')}:{declaring?.FullName}.{member.Name}";

        return Members.Value.TryGetValue(key, out var text) ? text
            : member is PropertyInfo && FrameworkSummaries.TryGetValue(member.Name, out var fallback) ? fallback
            : null;
    }

    /// <summary>Nom lisible d'un composant : <c>UiInput&lt;TValue&gt;</c>.</summary>
    public static string DisplayName(Type type)
    {
        if (!type.IsGenericType) return type.Name;
        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(a => a.Name))}>";
    }

    /// <summary>
    /// Paramètres d'un composant, ceux du composant d'abord puis ceux de ses classes de base ;
    /// hors <c>Class</c>, <c>Style</c> et attributs libres sauf si <paramref name="includeCommon"/>.
    /// </summary>
    public static IReadOnlyList<ApiParameter> Parameters(Type type, bool includeCommon = false)
    {
        var instance = TryCreate(type);
        var nullability = new NullabilityInfoContext();
        var seen = new HashSet<string>();
        var result = new List<ApiParameter>();

        for (var current = type; current is not null && current != typeof(object) && current != typeof(ComponentBase); current = current.BaseType)
        {
            if (!includeCommon && IsCommonBase(current)) break;

            foreach (var property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.GetCustomAttribute<ParameterAttribute>() is null || !seen.Add(property.Name)) continue;

                var propertyType = property.PropertyType;
                result.Add(new ApiParameter(
                    property.Name,
                    TypeName(propertyType, SafeNullability(nullability, property)),
                    FormatDefault(instance, property),
                    Summary(property) ?? string.Empty,
                    property.GetCustomAttribute<EditorRequiredAttribute>() is not null,
                    KindOf(propertyType),
                    current));
            }
        }

        return result;
    }

    /// <summary>Vrai si le composant hérite de <see cref="UiComponentBase"/> (donc accepte <c>Class</c>, <c>Style</c> et attributs libres).</summary>
    public static bool HasCommonParameters(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsCommonBase(current)) return true;
        }
        return false;
    }

    private static bool IsCommonBase(Type type) => (type.IsGenericType ? type.GetGenericTypeDefinition() : type) == typeof(UiComponentBase);

    private static ApiParameterKind KindOf(Type type)
    {
        var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        if (definition == typeof(EventCallback) || definition == typeof(EventCallback<>)) return ApiParameterKind.Event;
        if (definition == typeof(RenderFragment) || definition == typeof(RenderFragment<>)) return ApiParameterKind.Content;
        return ApiParameterKind.Parameter;
    }

    // ---------- Noms de types ----------
    private static readonly Dictionary<Type, string> Keywords = new()
    {
        [typeof(string)] = "string", [typeof(bool)] = "bool", [typeof(int)] = "int", [typeof(long)] = "long",
        [typeof(double)] = "double", [typeof(float)] = "float", [typeof(decimal)] = "decimal", [typeof(char)] = "char",
        [typeof(object)] = "object", [typeof(byte)] = "byte", [typeof(short)] = "short", [typeof(uint)] = "uint",
        [typeof(ulong)] = "ulong", [typeof(void)] = "void"
    };

    public static string TypeName(Type type, NullabilityInfo? info = null)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null) return TypeName(underlying, null) + "?";

        var name = Core(type, info);
        return !type.IsValueType && info?.ReadState == NullabilityState.Nullable ? name + "?" : name;
    }

    private static string Core(Type type, NullabilityInfo? info)
    {
        if (Keywords.TryGetValue(type, out var keyword)) return keyword;
        if (type.IsArray) return TypeName(type.GetElementType()!, info?.ElementType) + "[]";
        if (type.IsGenericParameter) return type.Name;
        if (!type.IsGenericType) return type.Name;

        var name = type.Name[..type.Name.IndexOf('`')];
        var arguments = type.GetGenericArguments();
        var rendered = arguments.Select((argument, index) =>
            TypeName(argument, info is not null && index < info.GenericTypeArguments.Length ? info.GenericTypeArguments[index] : null));
        return $"{name}<{string.Join(", ", rendered)}>";
    }

    private static NullabilityInfo? SafeNullability(NullabilityInfoContext context, PropertyInfo property)
    {
        try { return context.Create(property); }
        catch (Exception) { return null; }
    }

    // ---------- Valeurs par défaut ----------
    private static object? TryCreate(Type type)
    {
        try
        {
            var concrete = type.IsGenericTypeDefinition
                ? type.MakeGenericType(type.GetGenericArguments().Select(_ => typeof(object)).ToArray())
                : type;
            return Activator.CreateInstance(concrete);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? FormatDefault(object? instance, PropertyInfo declared)
    {
        if (instance is null) return null;

        try
        {
            var property = instance.GetType().GetProperty(declared.Name);
            var value = property?.GetValue(instance);
            return value switch
            {
                null => null,
                string { Length: 0 } => "\"\"",
                string text when text.Length <= 40 => $"\"{text}\"",
                bool flag => flag ? "true" : "false",
                char c => $"'{c}'",
                Enum e => $"{e.GetType().Name}.{e}",
                IFormattable number when value.GetType().IsPrimitive || value is decimal => number.ToString(null, CultureInfo.InvariantCulture),
                _ => null
            };
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ---------- Fichier XML ----------
    private static IReadOnlyDictionary<string, string> LoadMembers()
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using var stream = typeof(ApiDocs).Assembly.GetManifestResourceStream("WaesesUI.xml");
        if (stream is null) return result;

        var document = XDocument.Load(stream);
        foreach (var member in document.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            var summary = member.Element("summary");
            if (name is null || summary is null) continue;

            var text = ToPlainText(summary);
            if (text.Length > 0) result[name] = text;
        }
        return result;
    }

    internal static string ToPlainText(XElement element)
    {
        var builder = new StringBuilder();
        foreach (var node in element.Nodes()) Append(node, builder);
        return WhiteSpace().Replace(builder.ToString(), " ").Trim();
    }

    private static void Append(XNode node, StringBuilder builder)
    {
        switch (node)
        {
            case XText text:
                builder.Append(text.Value);
                break;
            case XElement element:
                switch (element.Name.LocalName)
                {
                    case "c" or "code":
                        builder.Append('`').Append(WhiteSpace().Replace(element.Value, " ").Trim()).Append('`');
                        break;
                    case "see":
                        var label = element.Value.Length > 0 ? element.Value : ShortName(element.Attribute("cref")?.Value);
                        builder.Append('`').Append(label).Append('`');
                        break;
                    case "paramref" or "typeparamref":
                        builder.Append('`').Append(element.Attribute("name")?.Value).Append('`');
                        break;
                    case "para":
                        builder.Append(' ');
                        foreach (var child in element.Nodes()) Append(child, builder);
                        builder.Append(' ');
                        break;
                    default:
                        foreach (var child in element.Nodes()) Append(child, builder);
                        break;
                }
                break;
        }
    }

    /// <summary><c>P:WaesesUI.UiSelect`1.ChildContent</c> devient <c>ChildContent</c>.</summary>
    internal static string ShortName(string? cref)
    {
        if (string.IsNullOrEmpty(cref)) return string.Empty;
        var name = cref[(cref.IndexOf(':') + 1)..];
        var paren = name.IndexOf('(');
        if (paren >= 0) name = name[..paren];
        name = name[(name.LastIndexOf('.') + 1)..];
        var tick = name.IndexOf('`');
        return tick >= 0 ? name[..tick] : name;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}
