using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace WaesesUIDocs.E2E;

[Collection(DocsSiteCollection.Name)]
public sealed class TreeViewTests(DocsSiteFixture site)
{
    [Fact]
    public async Task Larborescence_se_navigue_et_se_selectionne_au_clavier()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/arborescence");
        var tree = session.Page.GetByRole(AriaRole.Tree, new PageGetByRoleOptions { Name = "Dossiers du projet" });
        await tree.FocusAsync();
        await session.Page.Keyboard.PressAsync("ArrowDown");
        await session.Page.Keyboard.PressAsync("Enter");

        await Expect(tree.Locator("[role=treeitem][aria-selected=true]")).ToContainTextAsync("Composants");
        Assert.Empty(session.Problems);
    }
}
