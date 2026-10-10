using Microsoft.Playwright;

namespace WaesesUIDocs.E2E;

[Collection(DocsSiteCollection.Name)]
public sealed class SmokeTests(DocsSiteFixture site)
{
    [Fact]
    public async Task L_accueil_demarre_en_WebAssembly_et_affiche_son_titre_sans_erreur()
    {
        await using var session = await DocsSession.OpenAsync(site);

        Assert.Equal("WaesesUI", (await session.Page.Locator("article.docs-content h1").InnerTextAsync()).Trim());
        Assert.Empty(session.Problems);
    }
}
