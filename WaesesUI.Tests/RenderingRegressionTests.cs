using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace WaesesUI.Tests;

/// <summary>Régressions trouvées dans un vrai navigateur par les tests de bout en bout de la documentation.</summary>
public sealed class RenderingRegressionTests : TestContext
{
    public RenderingRegressionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiRadioGroup<string>> RenderRadios(string? value, Action<string?>? onChange = null) =>
        RenderComponent<UiRadioGroup<string>>(p => p
            .Add(c => c.Label, "Formule")
            .Add(c => c.Value, value)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => onChange?.Invoke(v)))
            .Add(c => c.ChildContent, b =>
            {
                var seq = 0;
                foreach (var (v, label, disabled) in new[] { ("a", "Alpha", false), ("b", "Beta", false), ("c", "Gamma", true) })
                {
                    b.OpenComponent<UiRadioOption<string>>(seq++);
                    b.AddAttribute(seq++, "Value", v);
                    b.AddAttribute(seq++, "Label", label);
                    b.AddAttribute(seq++, "Disabled", disabled);
                    b.CloseComponent();
                }
            }));

    private static string[] Checked(IRenderedComponent<UiRadioGroup<string>> cut) =>
        cut.FindAll("[role=radio]").Select(r => r.GetAttribute("aria-checked")!).ToArray();

    // ---------- Radios : un seul coché à la fois ----------
    [Fact]
    public void Choisir_une_option_decoche_l_ancienne_meme_si_ses_parametres_n_ont_pas_change()
    {
        string? chosen = "a";
        var cut = RenderRadios("a", v => chosen = v);
        Assert.Equal(new[] { "true", "false", "false" }, Checked(cut));

        cut.FindAll("[role=radio]")[1].Click();

        Assert.Equal("b", chosen);
        // Avant le correctif : « true, true, false » (l'option « Alpha » n'était pas redessinée).
        Assert.Equal(new[] { "false", "true", "false" }, Checked(cut));
    }

    [Fact]
    public void Un_changement_de_valeur_venu_du_parent_met_a_jour_toutes_les_options()
    {
        var cut = RenderRadios("a");

        cut.SetParametersAndRender(p => p.Add(c => c.Value, "b"));

        Assert.Equal(new[] { "false", "true", "false" }, Checked(cut));
        Assert.Equal(new[] { "-1", "0", "-1" }, cut.FindAll("[role=radio]").Select(r => r.GetAttribute("tabindex")!).ToArray());   // un seul arrêt de tabulation
    }

    [Fact]
    public void La_fleche_du_clavier_deplace_la_selection_en_sautant_les_options_desactivees()
    {
        string? chosen = "a";
        var cut = RenderRadios("a", v => chosen = v);

        cut.FindAll("[role=radio]")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal("b", chosen);
        Assert.Equal(new[] { "false", "true", "false" }, Checked(cut));

        cut.FindAll("[role=radio]")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });   // « Gamma » est désactivée : retour à « Alpha »
        Assert.Equal("a", chosen);
        Assert.Equal(new[] { "true", "false", "false" }, Checked(cut));
    }

    // ---------- Bouton de copie ----------
    [Fact]
    public async Task Le_bouton_de_copie_affiche_copie_puis_revient_au_libelle()
    {
        var copied = 0;
        var cut = RenderComponent<UiCopyButton>(p => p
            .Add(c => c.Text, "x").Add(c => c.ShowLabel, true)
            .Add(c => c.OnCopied, EventCallback.Factory.Create(this, () => copied++)));
        Assert.Contains("Copier", cut.Find("button").TextContent);

        await cut.InvokeAsync(() => cut.Find("button").Click());

        cut.WaitForAssertion(() => Assert.Contains("Copié", cut.Find("button").TextContent), TimeSpan.FromSeconds(1));
        Assert.Equal(1, copied);
        cut.WaitForAssertion(() => Assert.Contains("Copier", cut.Find("button").TextContent), TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void Un_presse_papiers_refuse_n_annonce_pas_copie_et_ne_leve_pas_d_exception()
    {
        JSInterop.SetupVoid("UiInterop.copyText", _ => true).SetException(new JSException("NotAllowedError"));
        var copied = 0;
        var cut = RenderComponent<UiCopyButton>(p => p
            .Add(c => c.Text, "x").Add(c => c.ShowLabel, true)
            .Add(c => c.OnCopied, EventCallback.Factory.Create(this, () => copied++)));

        cut.Find("button").Click();

        Assert.Equal(0, copied);
        Assert.Contains("Copier", cut.Find("button").TextContent);
        Assert.DoesNotContain("Copié", cut.Find("button").TextContent);
    }

    [Fact]
    public void Liberer_le_bouton_de_copie_pendant_l_attente_ne_leve_rien()
    {
        var cut = RenderComponent<UiCopyButton>(p => p.Add(c => c.Text, "x"));
        cut.Find("button").Click();

        var ex = Record.Exception(() => DisposeComponents());

        Assert.Null(ex);
    }

    [Fact]
    public void Image_reste_dans_la_mise_en_page_pendant_le_chargement_et_bascule_sur_le_secours()
    {
        var cut = RenderComponent<UiImage>(p => p.Add(c => c.Src, "x.png").Add(c => c.FallbackSrc, "f.png"));

        Assert.DoesNotContain("display", cut.Find("img").GetAttribute("style") ?? "");
        Assert.Contains("ui-image-loading", cut.Find("img").ClassName);

        cut.Find("img").TriggerEvent("onerror", EventArgs.Empty);
        Assert.Equal("f.png", cut.Find("img").GetAttribute("src"));
        Assert.DoesNotContain("ui-image-loading", cut.Find("img").ClassName);
    }
}
