using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class FoundationTests : TestContext
{
    public FoundationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- CssBuilder / StyleBuilder ----------
    [Fact]
    public void CssBuilder_ignore_les_valeurs_vides_et_les_conditions_fausses()
    {
        var css = CssBuilder.Default("a").Add(null).Add(" ").AddIf("b", false).AddIf("c", true).Add("d").Build();
        Assert.Equal("a c d", css);
    }

    [Fact]
    public void StyleBuilder_normalise_les_points_virgules()
    {
        Assert.Equal("color: red; width: 1px;", StyleBuilder.Default("color: red").Add("width: 1px;").Build());
        Assert.Null(StyleBuilder.Default().Build());
    }

    // ---------- UiButton ----------
    [Fact]
    public void Button_fusionne_Class_et_Style_et_les_attributs_libres()
    {
        var cut = RenderComponent<UiButton>(p => p
            .Add(c => c.Text, "Go").Add(c => c.Class, "extra").Add(c => c.Style, "margin:0")
            .AddUnmatched("data-x", "1"));
        var btn = cut.Find("button");
        Assert.Contains("ui-btn-primary", btn.ClassName);
        Assert.Contains("extra", btn.ClassName);
        Assert.Equal("margin:0", btn.GetAttribute("style"));
        Assert.Equal("1", btn.GetAttribute("data-x"));
    }

    [Fact]
    public void Lien_desactive_n_a_plus_de_href_et_expose_aria_disabled()
    {
        var cut = RenderComponent<UiButton>(p => p.Add(c => c.Text, "Go").Add(c => c.Href, "/x").Add(c => c.Disabled, true));
        var a = cut.Find("a");
        Assert.Null(a.GetAttribute("href"));
        Assert.Equal("true", a.GetAttribute("aria-disabled"));
        Assert.Equal("-1", a.GetAttribute("tabindex"));
        Assert.Contains("ui-btn-disabled", a.ClassName);
    }

    [Fact]
    public void Lien_actif_garde_href_et_securise_target_blank()
    {
        var cut = RenderComponent<UiButton>(p => p.Add(c => c.Text, "Go").Add(c => c.Href, "/x").Add(c => c.Target, "_blank"));
        var a = cut.Find("a");
        Assert.Equal("/x", a.GetAttribute("href"));
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
        Assert.Null(a.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Button_en_chargement_est_desactive_et_aria_busy()
    {
        var cut = RenderComponent<UiButton>(p => p.Add(c => c.Text, "Go").Add(c => c.Loading, true));
        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        Assert.Equal("true", btn.GetAttribute("aria-busy"));
    }

    // ---------- UiOptions ----------
    [Fact]
    public void Les_options_configurees_deviennent_les_valeurs_par_defaut_des_toasts()
    {
        using var ctx = new TestContext();
        ctx.Services.AddWaesesUIServices(o => { o.ToastDurationMs = 1234; o.MaxToasts = 2; });
        var toasts = ctx.Services.GetRequiredService<IToastService>();

        toasts.ShowToast("a");
        toasts.ShowToast("b");
        toasts.ShowToast("c");

        Assert.Equal(2, toasts.Toasts.Count);
        Assert.All(toasts.Toasts, t => Assert.Equal(1234, t.DurationMs));
        Assert.Equal("b", toasts.Toasts[0].Message);
    }

    // ---------- Dialogs ----------
    private sealed class SampleDialog : ComponentBase
    {
        [CascadingParameter] public UiDialogInstance Dialog { get; set; } = default!;
        [Parameter] public string Label { get; set; } = "";

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder b)
        {
            b.OpenElement(0, "button");
            b.AddAttribute(1, "class", "sample-ok");
            b.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => Dialog.Close(Label)));
            b.AddContent(3, Label);
            b.CloseElement();
        }
    }

    [Fact]
    public async Task Dialog_affiche_le_composant_et_renvoie_la_donnee_a_la_fermeture()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var service = Services.GetRequiredService<IUiDialogService>();

        var reference = await cut.InvokeAsync(() => service.ShowAsync<SampleDialog>("Titre", new UiDialogParameters().With("Label", "Valider")));

        cut.WaitForAssertion(() => Assert.Equal("Titre", cut.Find(".ui-modal-title").TextContent.Trim()));
        Assert.Equal("dialog", cut.Find(".ui-modal-box").GetAttribute("role"));

        cut.Find(".sample-ok").Click();
        var result = await reference.Result;

        Assert.False(result.Canceled);
        Assert.Equal("Valider", result.GetData<string>());
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".ui-modal-box")));
    }

    [Fact]
    public async Task Dialog_annule_par_le_bouton_fermer()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var service = Services.GetRequiredService<IUiDialogService>();
        var reference = await cut.InvokeAsync(() => service.ShowAsync<SampleDialog>("T"));
        cut.WaitForAssertion(() => cut.Find(".ui-modal-close-btn"));

        cut.Find(".ui-modal-close-btn").Click();

        Assert.True((await reference.Result).Canceled);
    }

    [Fact]
    public async Task Dialog_sans_provider_leve_une_erreur_explicite()
    {
        var service = Services.GetRequiredService<IUiDialogService>();
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ShowAsync<SampleDialog>());
        Assert.Contains("UiDialogProvider", ex.Message);
    }

    [Fact]
    public async Task Le_defilement_est_deverrouille_pour_chaque_dialogue_ferme()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var service = Services.GetRequiredService<IUiDialogService>();
        var reference = await cut.InvokeAsync(() => service.ShowAsync<SampleDialog>("T"));
        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.lockBodyScroll"));

        reference.Cancel();

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.unlockBodyScroll"));
    }
}
