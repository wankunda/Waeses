using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

/// <summary>Service de dialogue façon MudBlazor : options, UiDialog, instance en cascade, boîte de message, dialogue en ligne.</summary>
public sealed class DialogServiceTests : TestContext
{
    public DialogServiceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IUiDialogService Service => Services.GetRequiredService<IUiDialogService>();

    /// <summary>Composant de dialogue qui reçoit l'interface en cascade et dessine son contenu avec UiDialog.</summary>
    private sealed class LayoutDialog : ComponentBase
    {
        [CascadingParameter] public IUiDialogInstance Dialog { get; set; } = default!;
        [Parameter] public string Label { get; set; } = "";
        [Parameter] public EventCallback OnBackdrop { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<UiDialog>(0);
            b.AddAttribute(1, nameof(UiDialog.TitleContent), (RenderFragment)(t => t.AddMarkupContent(0, "<em class=\"custom-title\">Titre riche</em>")));
            b.AddAttribute(2, nameof(UiDialog.DialogContent), (RenderFragment)(c => c.AddMarkupContent(0, $"<p class=\"content\">{Label}</p>")));
            b.AddAttribute(3, nameof(UiDialog.DialogActions), (RenderFragment)(a =>
            {
                a.OpenElement(0, "button");
                a.AddAttribute(1, "class", "ok");
                a.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => Dialog.Close(UiDialogResult.Ok(42))));
                a.CloseElement();
                a.OpenElement(3, "button");
                a.AddAttribute(4, "class", "grow");
                a.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => Dialog.SetOptions(new UiDialogOptions { Size = UiSize.Xl })));
                a.CloseElement();
                a.OpenElement(6, "button");
                a.AddAttribute(7, "class", "rename");
                a.AddAttribute(8, "onclick", EventCallback.Factory.Create(this, () => Dialog.SetTitle("Nouveau")));
                a.CloseElement();
            }));
            b.AddAttribute(4, nameof(UiDialog.ActionsClass), "my-actions");
            b.AddAttribute(5, nameof(UiDialog.Gutters), false);
            if (OnBackdrop.HasDelegate) b.AddAttribute(6, nameof(UiDialog.OnBackdropClick), OnBackdrop);
            b.CloseComponent();
        }
    }

    private sealed class EmptyDialog : ComponentBase
    {
        [CascadingParameter] public UiDialogInstance Dialog { get; set; } = default!;
    }

    // ---------- Paramètres et résultat ----------
    [Fact]
    public void Les_parametres_types_utilisent_le_nom_de_la_propriete()
    {
        var parameters = new UiDialogParameters<LayoutDialog>().Add(x => x.Label, "Bonjour");

        Assert.Equal("Bonjour", parameters["Label"]);
        Assert.Equal("Bonjour", parameters.Get<string>("Label"));
        Assert.Equal(0, parameters.Get<int>("Absent"));
    }

    [Fact]
    public void Le_resultat_type_garde_le_type_de_la_donnee()
    {
        Assert.Equal(typeof(int), UiDialogResult.Ok(5).DataType);
        Assert.Null(UiDialogResult.Ok().DataType);
        Assert.True(UiDialogResult.Cancel().Canceled);
        Assert.Equal(5, UiDialogResult.Ok(5).GetData<int>());
    }

    [Fact]
    public async Task ShowAsync_par_type_refuse_un_type_qui_n_est_pas_un_composant()
    {
        RenderComponent<UiDialogProvider>();
        await Assert.ThrowsAsync<ArgumentException>(() => Service.ShowAsync(typeof(string)));
    }

    // ---------- UiDialog dans un composant affiché par le service ----------
    [Fact]
    public async Task UiDialog_dessine_en_tete_contenu_et_actions_et_le_provider_n_ajoute_pas_son_en_tete()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var reference = await cut.InvokeAsync(() => Service.ShowAsync(typeof(LayoutDialog), "Titre simple", new UiDialogParameters<LayoutDialog>().Add(x => x.Label, "Texte")));

        cut.WaitForAssertion(() => cut.Find(".custom-title"));
        Assert.Single(cut.FindAll(".ui-modal-header"));
        var title = cut.Find(".ui-modal-title");
        Assert.Equal(cut.Find(".ui-modal-box").GetAttribute("aria-labelledby"), title.Id);
        Assert.Equal("Texte", cut.Find(".ui-modal-body .content").TextContent);
        Assert.Contains("ui-modal-body-flush", cut.Find(".ui-modal-body").ClassName);
        Assert.Contains("my-actions", cut.Find(".ui-modal-footer").ClassName);
        Assert.IsType<LayoutDialog>(reference.Dialog);

        cut.Find(".ok").Click();
        Assert.Equal(42, await reference.GetReturnValueAsync<int>());
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".ui-modal-box")));
    }

    [Fact]
    public async Task Le_composant_change_les_options_et_le_titre_du_dialogue_ouvert()
    {
        var cut = RenderComponent<UiDialogProvider>();
        await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("Avant"));
        cut.WaitForAssertion(() => cut.Find(".ui-modal-box"));

        var instance = cut.FindComponent<EmptyDialog>().Instance.Dialog;
        await cut.InvokeAsync(() => instance.SetOptions(new UiDialogOptions { Size = UiSize.Xl }));
        await cut.InvokeAsync(() => instance.SetTitle("Après"));

        cut.WaitForAssertion(() => Assert.Contains("ui-modal-xl", cut.Find(".ui-modal-box").ClassName));
        Assert.Contains("max-width: 1000px", cut.Find(".ui-modal-box").GetAttribute("style"));
        Assert.Equal("Après", cut.Find(".ui-modal-title").TextContent.Trim());
    }

    // ---------- Options ----------
    [Fact]
    public async Task Les_reglages_du_provider_servent_de_valeurs_par_defaut_et_les_options_les_remplacent()
    {
        var cut = RenderComponent<UiDialogProvider>(p => p
            .Add(c => c.Position, UiDialogPosition.TopCenter)
            .Add(c => c.BackdropClass, "flou")
            .Add(c => c.ShowCloseButton, false));

        await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("A"));
        await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("B", options: new UiDialogOptions { Position = UiDialogPosition.BottomRight, ShowCloseButton = true }));

        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".ui-modal-backdrop").Count));
        var backdrops = cut.FindAll(".ui-modal-backdrop");
        Assert.Contains("ui-modal-pos-top", backdrops[0].ClassName);
        Assert.Contains("flou", backdrops[0].ClassName);
        Assert.Contains("ui-modal-pos-bottom", backdrops[1].ClassName);
        Assert.Contains("ui-modal-pos-right", backdrops[1].ClassName);
        Assert.Single(cut.FindAll(".ui-modal-close-btn"));
    }

    [Fact]
    public async Task Plein_ecran_sans_largeur_maximale_et_sans_en_tete()
    {
        var cut = RenderComponent<UiDialogProvider>();
        await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("Titre", options: new UiDialogOptions { FullScreen = true, NoHeader = true, Class = "ma-boite" }));

        cut.WaitForAssertion(() => cut.Find(".ui-modal-box"));
        var box = cut.Find(".ui-modal-box");
        Assert.Contains("ui-modal-fullscreen", box.ClassName);
        Assert.Contains("ma-boite", box.ClassName);
        Assert.Null(box.GetAttribute("style"));
        Assert.Contains("ui-modal-backdrop-fullscreen", cut.Find(".ui-modal-backdrop").ClassName);
        Assert.Empty(cut.FindAll(".ui-modal-header"));
    }

    [Fact]
    public async Task Le_clic_sur_le_fond_ne_ferme_pas_quand_l_option_est_desactivee()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var reference = await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("T", options: new UiDialogOptions { CloseOnBackdropClick = false }));
        cut.WaitForAssertion(() => cut.Find(".ui-modal-backdrop"));

        cut.Find(".ui-modal-backdrop").Click();

        Assert.False(reference.Result.IsCompleted);
        Assert.Single(cut.FindAll(".ui-modal-box"));
    }

    [Fact]
    public async Task OnBackdropClick_remplace_la_fermeture_automatique()
    {
        var clicks = 0;
        var cut = RenderComponent<UiDialogProvider>();
        var parameters = new UiDialogParameters<LayoutDialog>().Add(x => x.OnBackdrop, EventCallback.Factory.Create(this, () => clicks++));
        var reference = await cut.InvokeAsync(() => Service.ShowAsync<LayoutDialog>("T", parameters));
        cut.WaitForAssertion(() => cut.Find(".custom-title"));

        cut.Find(".ui-modal-backdrop").Click();

        Assert.Equal(1, clicks);
        Assert.False(reference.Result.IsCompleted);
    }

    [Fact]
    public async Task Le_focus_initial_est_transmis_au_javascript()
    {
        var cut = RenderComponent<UiDialogProvider>();
        await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("T", options: new UiDialogOptions { DefaultFocus = UiDefaultFocus.LastChild }));

        cut.WaitForAssertion(() => Assert.Single(JSInterop.Invocations, i => i.Identifier == "UiInterop.setupModal"));
        var options = JSInterop.Invocations.Single(i => i.Identifier == "UiInterop.setupModal").Arguments[2]!;
        Assert.Equal("lastChild", options.GetType().GetProperty("defaultFocus")!.GetValue(options));
    }

    // ---------- Service ----------
    [Fact]
    public async Task CancelAll_annule_tous_les_dialogues_ouverts()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var first = await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("1"));
        var second = await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("2"));
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll(".ui-modal-box").Count));

        await cut.InvokeAsync(() => Service.CancelAll());

        Assert.True((await first.Result).Canceled);
        Assert.True((await second.Result).Canceled);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".ui-modal-box")));
    }

    [Fact]
    public async Task Close_du_service_ferme_le_dialogue_avec_le_resultat()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var reference = await cut.InvokeAsync(() => Service.ShowAsync<EmptyDialog>("T"));

        await cut.InvokeAsync(() => Service.Close(reference, UiDialogResult.Ok("fini")));

        Assert.Equal("fini", (await reference.Result).GetData<string>());
    }

    [Theory]
    [InlineData(2, true)]   // Oui
    [InlineData(1, false)]  // Non
    [InlineData(0, null)]   // Annuler
    public async Task ShowMessageBoxAsync_renvoie_oui_non_ou_null(int button, bool? expected)
    {
        var cut = RenderComponent<UiDialogProvider>();
        var task = cut.InvokeAsync(() => Service.ShowMessageBoxAsync("Quitter ?", "Enregistrer avant ?", "Oui", "Non", "Annuler"));

        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll(".ui-modal-footer button").Count));
        Assert.Equal("Quitter ?", cut.Find(".ui-modal-title").TextContent.Trim());
        cut.FindAll(".ui-modal-footer button")[button].Click();

        Assert.Equal(expected, await task);
    }

    [Fact]
    public async Task ShowMessageBoxAsync_sans_texte_n_affiche_que_le_bouton_OK()
    {
        var cut = RenderComponent<UiDialogProvider>();
        var task = cut.InvokeAsync(() => Service.ShowMessageBoxAsync("Info", "Terminé."));

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".ui-modal-footer button")));
        Assert.Equal("OK", cut.Find(".ui-modal-footer button").TextContent.Trim());
        cut.Find(".ui-modal-close-btn").Click();

        Assert.Null(await task);
    }

    // ---------- Dialogue en ligne ----------
    [Fact]
    public void Le_dialogue_en_ligne_s_ouvre_avec_Visible_et_previent_la_page_a_la_fermeture()
    {
        var provider = RenderComponent<UiDialogProvider>();
        bool? visible = null;

        var inline = RenderComponent<UiDialog>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.VisibleChanged, v => visible = v)
            .Add(c => c.Title, "En ligne")
            .Add(c => c.Options, new UiDialogOptions { Size = UiSize.Sm })
            .Add(c => c.DialogContent, "<p class=\"inline-content\">Bonjour</p>"));

        provider.WaitForAssertion(() => provider.Find(".inline-content"));
        Assert.Equal("En ligne", provider.Find(".ui-modal-title").TextContent.Trim());
        Assert.Contains("ui-modal-sm", provider.Find(".ui-modal-box").ClassName);
        Assert.Empty(inline.Markup.Trim());

        provider.Find(".ui-modal-close-btn").Click();

        inline.WaitForAssertion(() => Assert.False(visible));
        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-modal-box")));
    }

    [Fact]
    public void Le_dialogue_en_ligne_se_met_a_jour_et_se_ferme_quand_la_page_le_demande()
    {
        var provider = RenderComponent<UiDialogProvider>();
        var inline = RenderComponent<UiDialog>(p => p
            .Add(c => c.Visible, true)
            .Add(c => c.DialogContent, "<p class=\"v1\">Un</p>"));
        provider.WaitForAssertion(() => provider.Find(".v1"));

        inline.SetParametersAndRender(p => p.Add(c => c.DialogContent, "<p class=\"v2\">Deux</p>"));
        provider.WaitForAssertion(() => provider.Find(".v2"));

        inline.SetParametersAndRender(p => p.Add(c => c.Visible, false));
        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-modal-box")));
    }

    // ---------- UiModal ----------
    [Fact]
    public void UiModal_accepte_position_plein_ecran_et_classe_du_fond()
    {
        var cut = RenderComponent<UiModal>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Position, UiDialogPosition.TopLeft)
            .Add(c => c.FullScreen, true)
            .Add(c => c.BackdropClass, "flou"));

        var backdrop = cut.Find(".ui-modal-backdrop");
        Assert.Contains("ui-modal-pos-top", backdrop.ClassName);
        Assert.Contains("ui-modal-pos-left", backdrop.ClassName);
        Assert.Contains("flou", backdrop.ClassName);
        Assert.Contains("ui-modal-fullscreen", cut.Find(".ui-modal-box").ClassName);
    }

    [Fact]
    public void UiModal_Xs_a_bien_une_largeur_de_320px()
    {
        var cut = RenderComponent<UiModal>(p => p.Add(c => c.IsOpen, true).Add(c => c.Size, UiSize.Xs));
        Assert.Contains("max-width: 320px", cut.Find(".ui-modal-box").GetAttribute("style"));
    }
}
