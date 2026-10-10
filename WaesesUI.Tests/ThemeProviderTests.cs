using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

// Le fournisseur de thème réapplique le thème à chaque changement du paramètre Theme.
public sealed class ThemeProviderTests : TestContext
{
    public ThemeProviderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    [Fact]
    public void Changer_Theme_reapplique_le_theme()
    {
        var cut = RenderComponent<UiThemeProvider>(p => p.Add(c => c.Theme, UiTheme.Dark));
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.applyGlobalTheme"));

        cut.SetParametersAndRender(p => p.Add(c => c.Theme, UiTheme.Light));

        var appels = JSInterop.Invocations.Where(i => i.Identifier == "UiInterop.applyGlobalTheme").ToList();
        Assert.Equal(2, appels.Count);
        Assert.Equal("light", appels[1].Arguments[0]);
    }

    [Fact]
    public void Changer_Theme_plusieurs_fois_reapplique_a_chaque_fois()
    {
        var cut = RenderComponent<UiThemeProvider>(p => p.Add(c => c.Theme, UiTheme.Dark));
        cut.SetParametersAndRender(p => p.Add(c => c.Theme, UiTheme.Light));
        cut.SetParametersAndRender(p => p.Add(c => c.Theme, UiTheme.Dark));
        cut.SetParametersAndRender(p => p.Add(c => c.Theme, UiTheme.Light));

        var themes = JSInterop.Invocations.Where(i => i.Identifier == "UiInterop.applyGlobalTheme")
            .Select(i => (string)i.Arguments[0]!).ToList();
        Assert.Equal(new[] { "dark", "light", "dark", "light" }, themes);
    }
}
