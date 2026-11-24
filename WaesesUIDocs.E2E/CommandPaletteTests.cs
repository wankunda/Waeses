using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace WaesesUIDocs.E2E;

[Collection(DocsSiteCollection.Name)]
public sealed class CommandPaletteTests(DocsSiteFixture site)
{
    [Fact]
    public async Task Le_raccourci_ouvre_la_palette_et_execute_une_commande_recherchee()
    {
        await using var session = await DocsSession.OpenAsync(site, "composants/palette-de-commandes");
        var dialog = session.Page.GetByRole(AriaRole.Dialog, new PageGetByRoleOptions { Name = "Commandes de démonstration" });
        await session.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await session.Page.Keyboard.PressAsync("Control+k");
        await Expect(dialog).ToBeVisibleAsync();

        await session.Page.GetByLabel("Rechercher une action").FillAsync("préférences");
        var options = session.Page.GetByRole(AriaRole.Option);
        await Expect(options).ToHaveCountAsync(1);
        await options.ClickAsync();

        await Expect(session.Page.GetByRole(AriaRole.Status)).ToHaveTextAsync("Action exécutée : Ouvrir les préférences");
        await Expect(dialog).ToHaveCountAsync(0);
        Assert.Empty(session.Problems);
    }
}
