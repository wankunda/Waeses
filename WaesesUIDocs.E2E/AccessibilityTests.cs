using Deque.AxeCore.Commons;
using Deque.AxeCore.Playwright;
using Microsoft.Playwright;
using WaesesUIDocs.Docs;

namespace WaesesUIDocs.E2E;

/// <summary>
/// Contrôle d'accessibilité automatique (axe-core, règles WCAG 2.0/2.1 A et AA) de chaque page, en mode clair et sombre.
/// Un contrôle automatique n'attrape qu'une partie des problèmes (contrastes, noms accessibles, rôles ARIA, structure) :
/// il ne remplace ni un test au clavier ni un test avec un lecteur d'écran.
/// </summary>
[Collection(DocsSiteCollection.Name)]
public sealed class AccessibilityTests(DocsSiteFixture site)
{
    // DOCS_E2E_AXE_STRICT=1 : fait aussi échouer les violations modérées et mineures (utile pour auditer, trop bruyant pour la CI).
    private static readonly string[] Blocking = Environment.GetEnvironmentVariable("DOCS_E2E_AXE_STRICT") == "1"
        ? new[] { "critical", "serious", "moderate", "minor" }
        : new[] { "critical", "serious" };

    public static IEnumerable<object[]> PagesAndSchemes() =>
        DocsCatalog.Entries.SelectMany(e => new[] { ColorScheme.Light, ColorScheme.Dark }.Select(scheme => new object[] { e.Href, scheme }));

    [Theory]
    [MemberData(nameof(PagesAndSchemes))]
    public async Task Aucune_violation_grave_ou_critique(string href, ColorScheme scheme)
    {
        await using var session = await DocsSession.OpenAsync(site, href, scheme: scheme);
        await session.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // theme.css anime les couleurs (transition sur *) : mesuré en plein fondu, un contraste serait faussé. On mesure les couleurs finales.
        await session.Page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = "*, *::before, *::after { transition: none !important; animation: none !important; }" });

        var results = await session.Page.RunAxe(new AxeRunOptions
        {
            RunOnly = new RunOnlyOptions { Type = "tag", Values = new List<string> { "wcag2a", "wcag2aa", "wcag21a", "wcag21aa" } }
        });

        var blocking = results.Violations.Where(v => Blocking.Contains(v.Impact)).ToList();
        Assert.True(blocking.Count == 0, Describe(href, scheme, blocking));
    }

    private static string Describe(string href, ColorScheme scheme, IEnumerable<AxeResultItem> violations)
    {
        var lines = violations.Select(v =>
            $"  [{v.Impact}] {v.Id} — {v.Help} ({v.Nodes.Length} élément(s))\n" +
            string.Join("\n", v.Nodes.Take(14).Select(n => $"      {n.Target}\n        {Truncate(n.Html)}")));
        return $"/{href} ({scheme}) :\n{string.Join("\n", lines)}";
    }

    private static string Truncate(string html) => html.Length <= 120 ? html : html[..120] + "…";
}
