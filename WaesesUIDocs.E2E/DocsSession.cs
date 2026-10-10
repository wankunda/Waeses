using Microsoft.Playwright;

namespace WaesesUIDocs.E2E;

/// <summary>Une page de navigateur ouverte sur le site, qui enregistre tout ce qui ne devrait jamais arriver (erreurs console, exceptions, requêtes échouées).</summary>
public sealed class DocsSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;

    private DocsSession(IBrowserContext context, IPage page, string baseUrl)
    {
        _context = context;
        Page = page;
        BaseUrl = baseUrl;
        Problems = new List<string>();

        page.Console += (_, message) =>
        {
            if (message.Type == "error") Problems.Add("console.error : " + message.Text);
        };
        page.PageError += (_, error) => Problems.Add("exception de page : " + error);
        page.RequestFailed += (_, request) => Problems.Add($"requête échouée : {request.Url} ({request.Failure})");
        page.Response += (_, response) =>
        {
            if (response.Status >= 400) Problems.Add($"HTTP {response.Status} : {response.Url}");
        };
    }

    public IPage Page { get; }
    public string BaseUrl { get; }

    /// <summary>Problèmes détectés depuis l'ouverture ; un test qui se veut propre vérifie qu'elle est vide.</summary>
    public List<string> Problems { get; }

    public static async Task<DocsSession> OpenAsync(DocsSiteFixture site, string path = "", int width = 1280, int height = 900, ColorScheme scheme = ColorScheme.Light)
    {
        var context = await site.Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = width, Height = height },
            ColorScheme = scheme,
            Locale = "fr-FR",
            Permissions = new[] { "clipboard-read", "clipboard-write" }
        });
        var page = await context.NewPageAsync();
        page.SetDefaultTimeout(30_000);

        var session = new DocsSession(context, page, site.BaseUrl);
        await session.GotoAsync(path);
        return session;
    }

    /// <summary>Charge une page du site et attend que l'application WebAssembly ait démarré et affiché son titre.</summary>
    public async Task GotoAsync(string path)
    {
        await Page.GotoAsync($"{BaseUrl}/{path.TrimStart('/')}");
        await WaitForReadyAsync();
    }

    public async Task WaitForReadyAsync() =>
        await Page.Locator("article.docs-content h1").WaitForAsync(new LocatorWaitForOptions { Timeout = 90_000 });

    /// <summary>Le bloc d'exemple placé sous le titre de section <paramref name="sectionTitle"/>.</summary>
    public ILocator Example(string sectionTitle) =>
        Page.Locator("section.docs-section", new PageLocatorOptions { Has = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = sectionTitle, Level = 2, Exact = true }) })
            .Locator(".doc-example").First;

    /// <summary>
    /// Le rendu de l'exemple, sans son code source : le code affiché contient les mêmes textes
    /// (messages, libellés) et ferait correspondre deux éléments à une même recherche.
    /// </summary>
    public ILocator Preview(string sectionTitle) => Example(sectionTitle).Locator(".doc-example-preview");

    public async ValueTask DisposeAsync() => await _context.CloseAsync();
}
