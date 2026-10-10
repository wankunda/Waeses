using AngleSharp.Dom;
using System.Reflection;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WaesesUIDocs.Docs;

namespace WaesesUI.Tests;

/// <summary>
/// Garde-fous du site de documentation : pages, exemples, liens et tableaux d'API restent cohérents entre eux.
/// </summary>
public sealed class DocsSiteTests : TestContext
{

    private static readonly Assembly DocsAssembly = typeof(DocsCatalog).Assembly;

    public DocsSiteTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- Découverte ----------
    private static IEnumerable<(Type Type, string Route)> Pages() => DocsAssembly.GetTypes()
        .Where(t => typeof(IComponent).IsAssignableFrom(t))
        .SelectMany(t => t.GetCustomAttributes<RouteAttribute>().Select(r => (Type: t, Route: r.Template.Trim('/'))));

    private static IEnumerable<Type> Examples() => DocsAssembly.GetTypes()
        .Where(t => typeof(IComponent).IsAssignableFrom(t) && t.Namespace?.StartsWith("WaesesUIDocs.Examples", StringComparison.Ordinal) == true && !t.IsAbstract);

    public static IEnumerable<object[]> PageData() => Pages().Select(p => new object[] { p.Type });
    public static IEnumerable<object[]> ExampleData() => Examples().Select(e => new object[] { e });

    // ---------- Catalogue <-> pages ----------
    [Fact]
    public void Chaque_page_du_catalogue_a_une_route_et_inversement()
    {
        var routes = Pages().Select(p => p.Route).OrderBy(r => r).ToList();
        var catalog = DocsCatalog.Entries.Select(e => e.Href).OrderBy(h => h).ToList();

        Assert.Equal(catalog, routes);
        Assert.Equal(routes.Count, routes.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(PageData))]
    public void Chaque_page_s_affiche_avec_le_titre_du_catalogue_et_sans_erreur(Type pageType)
    {
        var route = pageType.GetCustomAttributes<RouteAttribute>().First().Template.Trim('/');
        var entry = DocsCatalog.Entries.Single(e => e.Href == route);

        var cut = RenderPage(pageType);

        var heading = cut.Find("h1").TextContent.Trim();
        Assert.True(route.Length == 0 ? heading == "WaesesUI" : heading == entry.Title, $"{pageType.Name} : titre « {heading} » au lieu de « {entry.Title} »");
        Assert.DoesNotContain("L'exemple a échoué", cut.Markup);
        Assert.DoesNotContain("// Source introuvable pour", cut.Markup);       // message de DocExample quand un exemple n'a pas de fichier embarqué
        Assert.DoesNotContain("Aucune page", cut.Markup);
    }

    private IRenderedComponent<DynamicComponent> RenderPage(Type pageType) =>
        RenderComponent<DynamicComponent>(p => p.Add(c => c.Type, pageType));

    // ---------- Exemples ----------
    [Fact]
    public void Il_existe_des_exemples()
    {
        Assert.True(Examples().Count() >= 20, "Les exemples ne sont pas découverts (namespace WaesesUIDocs.Examples.*).");
    }

    [Theory]
    [MemberData(nameof(ExampleData))]
    public void Chaque_exemple_a_son_code_embarque_et_s_affiche_sans_exception(Type example)
    {
        var source = ExampleSource.For(example);
        Assert.False(string.IsNullOrWhiteSpace(source), $"{example.Name} : source non embarquée.");
        Assert.DoesNotContain('\r', source);

        var cut = RenderComponent<DynamicComponent>(p => p.Add(c => c.Type, example));
        Assert.NotEmpty(cut.Markup);
    }

    [Fact]
    public void DocExample_affiche_le_rendu_et_le_code_du_meme_fichier()
    {
        var cut = RenderComponent<DocExample>(p => p
            .Add(c => c.Example, Examples().First(e => e.Name == "ButtonColors"))
            .Add(c => c.Title, "Couleurs"));

        Assert.Contains("Couleurs", cut.Find(".doc-example-title").TextContent);
        Assert.NotEmpty(cut.FindAll(".doc-example-preview .ui-btn"));
        Assert.Contains("Enum.GetValues&lt;UiVariant&gt;()", cut.Find(".doc-example-code pre").InnerHtml);
    }

    [Fact]
    public void DocExample_affiche_les_fichiers_compagnons_a_la_suite()
    {
        var example = Examples().First(e => e.Name == "DialogServiceExample");
        var companion = Examples().First(e => e.Name == "EditNameDialog");

        var cut = RenderComponent<DocExample>(p => p.Add(c => c.Example, example).Add(c => c.Companions, new[] { companion }));

        var files = cut.FindAll(".doc-example-file").Select(f => f.TextContent).ToArray();
        Assert.Equal(new[] { "DialogServiceExample.razor", "EditNameDialog.razor" }, files);
        Assert.Equal(2, cut.FindAll(".ui-code-block").Count);
    }

    [Fact]
    public void Un_exemple_qui_echoue_n_emporte_pas_la_page()
    {
        var cut = RenderComponent<DocExample>(p => p.Add(c => c.Example, typeof(Failing)));

        Assert.Contains("L'exemple a échoué", cut.Markup);
        Assert.Contains("boom", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".doc-example-code"));
    }

    private sealed class Failing : ComponentBase
    {
        protected override void OnInitialized() => throw new InvalidOperationException("boom");
    }

    // ---------- Contenu ----------
    [Theory]
    [MemberData(nameof(PageData))]
    public void Les_pages_ne_contiennent_aucune_entite_HTML_doublement_encodee(Type pageType)
    {
        var cut = RenderPage(pageType);

        // « &amp;quot; », « &amp;lt; »… : signe d'un texte écrit avec des entités dans un paramètre de composant.
        Assert.DoesNotContain("&amp;quot;", cut.Markup);
        Assert.DoesNotContain("&amp;lt;", cut.Markup);
        Assert.DoesNotContain("&amp;gt;", cut.Markup);
        Assert.DoesNotContain("&amp;amp;", cut.Markup);
    }

    [Theory]
    [MemberData(nameof(PageData))]
    public void Les_pages_n_affichent_aucun_accent_grave_litteral_hors_blocs_de_code(Type pageType)
    {
        var cut = RenderPage(pageType);

        // Un `code` écrit dans du HTML brut s'afficherait tel quel : il faut <code> ou un paramètre passant par DocText.
        var visible = string.Concat(cut.Nodes.SelectMany(n => n.Descendants<AngleSharp.Dom.IText>())
            .Where(t => !t.ParentElement!.ClosestPre())
            .Select(t => t.Data));
        Assert.True(visible.Length > 100, "Le test n'a rien analysé.");
        Assert.DoesNotContain('`', visible);
    }

    [Fact]
    public void Tous_les_liens_internes_pointent_vers_une_page_existante()
    {
        var known = DocsCatalog.Entries.Select(e => e.Href).ToHashSet();
        var broken = new List<string>();

        foreach (var (type, route) in Pages())
        {
            var cut = RenderPage(type);
            foreach (var link in cut.FindAll("a[href]"))
            {
                var href = link.GetAttribute("href")!;
                if (href.Contains("://") || href.StartsWith('#') || href.StartsWith("mailto:") || href.StartsWith("data:") || href.StartsWith("blob:")) continue;

                var target = href.Trim('/');
                if (!known.Contains(target)) broken.Add($"{type.Name} -> {href}");
            }
        }

        Assert.True(broken.Count == 0, "Liens cassés : " + string.Join(", ", broken));
    }

    [Fact]
    public void Les_pages_de_composants_presentent_un_tableau_d_API_par_composant_declare()
    {
        var withApi = Pages().Select(p => p.Route).Where(r => r.StartsWith("composants/")).ToArray();   // toute page de composants a au moins un tableau d'API

        foreach (var (type, route) in Pages().Where(p => withApi.Contains(p.Route)))
        {
            var cut = RenderPage(type);
            var tables = cut.FindAll(".api-table");
            Assert.True(tables.Count >= 1, $"{route} : aucun tableau d'API.");

            // Chaque tableau liste des paramètres, ou dit explicitement que le composant n'en a pas.
            foreach (var table in tables)
            {
                Assert.True(table.QuerySelectorAll("tbody tr").Length > 0 || table.TextContent.Contains("n'a pas de paramètre"),
                    $"{route} : tableau d'API vide et sans explication.");
            }
        }
    }

    [Fact]
    public void Tous_les_composants_declares_dans_ApiComponents_apparaissent_dans_une_page()
    {
        var rendered = string.Concat(Pages().Select(p => RenderPage(p.Type).Markup));

        foreach (var component in DocsCatalog.ApiComponents)
        {
            var title = System.Net.WebUtility.HtmlEncode("<" + ApiDocs.DisplayName(component) + ">");
            Assert.True(rendered.Contains(title), $"{ApiDocs.DisplayName(component)} est déclaré dans ApiComponents mais n'est affiché dans aucune page.");
        }
    }
}

internal static class DomExtensions
{
    /// <summary>Vrai si l'élément est dans un bloc de code (<c>pre</c>), où les accents graves sont du contenu légitime.</summary>
    public static bool ClosestPre(this AngleSharp.Dom.IElement element)
    {
        for (var current = element; current is not null; current = current.ParentElement)
        {
            if (current.LocalName == "pre") return true;
        }
        return false;
    }
}
