using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace WaesesUI.Tests;

/// <summary>
/// Régression : pendant le prérendu statique, <c>JS.InvokeVoidAsync</c> lève <see cref="InvalidOperationException"/>
/// ("JavaScript interop calls cannot be issued at this time") ; à la fermeture d'un circuit, <see cref="JSDisconnectedException"/>.
/// Libérer un composant ne doit jamais propager ces erreurs (elles finissaient en « unhandled exception » dans les journaux Kestrel).
/// </summary>
public sealed class TeardownTests : TestContext
{
    public TeardownTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    public static IEnumerable<object[]> Failures()
    {
        yield return new object[] { new InvalidOperationException("JavaScript interop calls cannot be issued at this time.") };
        yield return new object[] { new JSDisconnectedException("Circuit fermé") };
        yield return new object[] { new TaskCanceledException() };
    }

    private void FailOn(string identifier, Exception exception) =>
        JSInterop.SetupVoid(identifier, _ => true).SetException(exception);

    private static async Task AssertDisposeDoesNotThrowAsync(object component)
    {
        var ex = await Record.ExceptionAsync(async () => await ((IAsyncDisposable)component).DisposeAsync());
        Assert.Null(ex);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task UiThemeProvider_se_libere_sans_erreur(Exception failure)
    {
        var cut = RenderComponent<UiThemeProvider>();
        FailOn("UiInterop.clearGlobalTheme", failure);

        await AssertDisposeDoesNotThrowAsync(cut.Instance);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task UiPopover_se_libere_sans_erreur(Exception failure)
    {
        var cut = RenderComponent<UiPopover>(p => p.Add(c => c.ChildContent, b => b.AddContent(0, "x")));
        FailOn("UiInterop.teardownOverlay", failure);

        await AssertDisposeDoesNotThrowAsync(cut.Instance);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task UiAutocomplete_se_libere_sans_erreur(Exception failure)
    {
        var current = 0;
        var cut = RenderComponent<UiAutocomplete<string, int>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.DisplaySelector, s => s)
            .Add(c => c.ValueSelector, s => 1)
            .Add(c => c.ValueExpression, () => current));
        FailOn("UiInterop.teardownAutocomplete", failure);

        await AssertDisposeDoesNotThrowAsync(cut.Instance);
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task UiMenu_se_libere_sans_erreur(Exception failure)
    {
        var cut = RenderComponent<UiMenu>(p => p.Add(c => c.ChildContent, b => b.AddContent(0, "x")));
        FailOn("UiInterop.teardownMenu", failure);

        await AssertDisposeDoesNotThrowAsync(cut.Instance);
    }

    [Fact]
    public async Task Un_composant_jamais_rendu_cote_client_n_appelle_pas_le_JS_a_la_liberation()
    {
        // Prérendu : OnAfterRender n'a pas eu lieu, donc rien n'a été installé et rien ne doit être démonté.
        var popover = new UiPopover();
        var invocations = JSInterop.Invocations.Count;

        await popover.DisposeAsync();

        Assert.Equal(invocations, JSInterop.Invocations.Count);
    }
}
