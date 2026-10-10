using System.Reflection;

namespace WaesesUIDocs.Docs;

/// <summary>Code source d'un exemple, lu dans les ressources embarquées : le code affiché est celui qui s'exécute.</summary>
public static class ExampleSource
{
    private static readonly Assembly DocsAssembly = typeof(ExampleSource).Assembly;

    // MSBuild écrit %(RecursiveDir) avec le séparateur du système (« Examples/Buttons\X.razor » sous Windows) : on normalise.
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Resources = new(() => DocsAssembly.GetManifestResourceNames()
        .Where(n => n.StartsWith("Examples/", StringComparison.Ordinal))
        .ToDictionary(n => n.Replace('\\', '/'), n => n, StringComparer.Ordinal));

    /// <summary>Noms des ressources d'exemples, avec des <c>/</c> comme séparateurs (<c>Examples/Dossier/Nom.razor</c>).</summary>
    public static IEnumerable<string> ResourceNames => Resources.Value.Keys;

    /// <summary>Source du composant d'exemple <paramref name="exampleType"/>, ou <c>null</c> s'il n'a pas de fichier embarqué.</summary>
    public static string? For(Type exampleType)
    {
        var suffix = "/" + exampleType.Name + ".razor";
        var normalized = ResourceNames.FirstOrDefault(n => n.EndsWith(suffix, StringComparison.Ordinal));
        if (normalized is null) return null;

        using var stream = DocsAssembly.GetManifestResourceStream(Resources.Value[normalized]);
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return Clean(reader.ReadToEnd());
    }

    internal static string Clean(string source) => source.TrimStart('﻿').Replace("\r\n", "\n").Trim('\n', ' ') + "\n";
}
