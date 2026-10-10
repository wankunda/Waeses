using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

/// <summary>Régressions trouvées par axe-core dans un vrai navigateur (voir les tests de bout en bout de la documentation).</summary>
public sealed class AccessibilityFixesTests : TestContext
{
    private sealed record Skill(string Code, string Name);

    private static readonly Skill[] Skills = { new("a", "Alpha"), new("b", "Beta"), new("c", "Gamma") };

    public AccessibilityFixesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiMultiSelect<Skill, string>> RenderMulti(IReadOnlyList<string> selected, bool disabled = false, Action<IReadOnlyList<string>>? onChange = null) =>
        RenderComponent<UiMultiSelect<Skill, string>>(p => p
            .Add(c => c.Items, Skills)
            .Add(c => c.DisplaySelector, s => s.Name)
            .Add(c => c.ValueSelector, s => s.Code)
            .Add(c => c.SelectedValues, selected)
            .Add(c => c.Disabled, disabled)
            .Add(c => c.Label, "Compétences")
            .Add(c => c.SelectedValuesChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, v => onChange?.Invoke(v))));

    // ---------- UiMultiSelect : aucun contrôle interactif imbriqué ----------
    [Fact]
    public void MultiSelect_n_imbrique_aucun_bouton_dans_un_bouton()
    {
        var cut = RenderMulti(new[] { "a", "b" });

        Assert.Empty(cut.FindAll("button button"));
        Assert.Equal(2, cut.FindAll(".ui-multiselect-chip button").Count);             // les « retirer » existent, hors du bouton de bascule
        Assert.Empty(cut.FindAll(".ui-multiselect-toggle .ui-multiselect-chip"));
    }

    [Fact]
    public void MultiSelect_le_bouton_de_bascule_expose_l_etat_et_resume_la_selection_aux_lecteurs_d_ecran()
    {
        var cut = RenderMulti(new[] { "a", "c" });

        var toggle = cut.Find("button.ui-multiselect-toggle");
        Assert.Equal("false", toggle.GetAttribute("aria-expanded"));
        Assert.Equal("listbox", toggle.GetAttribute("aria-haspopup"));
        Assert.Equal("Alpha, Gamma", cut.Find($"#{toggle.GetAttribute("aria-describedby")}").TextContent);

        cut.Find(".ui-multiselect-trigger").Click();
        Assert.Equal("true", cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void MultiSelect_sans_selection_le_resume_est_le_texte_indicatif()
    {
        var cut = RenderMulti(Array.Empty<string>());

        var summary = cut.Find($"#{cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-describedby")}");
        Assert.Equal("Sélectionner...", summary.TextContent);
        Assert.Equal("Sélectionner...", cut.Find(".ui-multiselect-placeholder").TextContent);
    }

    [Fact]
    public void MultiSelect_retirer_une_puce_met_a_jour_la_selection_sans_ouvrir_la_liste()
    {
        IReadOnlyList<string>? selected = null;
        var cut = RenderMulti(new[] { "a", "b" }, onChange: v => selected = v);

        cut.Find("button[aria-label='Retirer Alpha']").Click();

        Assert.Equal(new[] { "b" }, selected);
        Assert.Equal("false", cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void MultiSelect_desactive_ne_s_ouvre_pas_et_desactive_ses_boutons()
    {
        var cut = RenderMulti(new[] { "a" }, disabled: true);

        cut.Find(".ui-multiselect-trigger").Click();

        Assert.True(cut.Find("button.ui-multiselect-toggle").HasAttribute("disabled"));
        Assert.True(cut.Find(".ui-multiselect-chip button").HasAttribute("disabled"));
        Assert.Contains("ui-multiselect-disabled", cut.Find(".ui-multiselect-trigger").ClassName);
        Assert.Equal("false", cut.Find("button.ui-multiselect-toggle").GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll("[role=listbox]"));
    }

    // ---------- Cases et interrupteurs désactivés ----------
    [Fact]
    public void Une_case_ou_un_interrupteur_desactive_declare_aria_disabled_sinon_pas()
    {
        var flag = false;
        var disabledBox = RenderComponent<UiCheckbox>(p => p.Add(c => c.ValueExpression, () => flag).Add(c => c.Disabled, true));
        var disabledSwitch = RenderComponent<UiSwitch>(p => p.Add(c => c.ValueExpression, () => flag).Add(c => c.Disabled, true));
        var enabledBox = RenderComponent<UiCheckbox>(p => p.Add(c => c.ValueExpression, () => flag));

        Assert.Equal("true", disabledBox.Find("[role=checkbox]").GetAttribute("aria-disabled"));
        Assert.Equal("true", disabledSwitch.Find("[role=switch]").GetAttribute("aria-disabled"));
        Assert.Null(enabledBox.Find("[role=checkbox]").GetAttribute("aria-disabled"));
    }

    // ---------- UiInputFile : nom accessible du champ de fichier ----------
    [Theory]
    [InlineData(FileUploadVariant.Dropzone, "Pièces jointes", null, "Pièces jointes")]
    [InlineData(FileUploadVariant.Button, null, "Importer un justificatif", "Importer un justificatif")]
    [InlineData(FileUploadVariant.Dropzone, null, null, "Importer un fichier")]               // ButtonText a une valeur par défaut
    [InlineData(FileUploadVariant.Button, "Photo", "Autre texte", "Photo")]                   // le libellé prime sur le texte du bouton
    public void Le_champ_de_fichier_a_toujours_un_nom_accessible(FileUploadVariant variant, string? label, string? buttonText, string expected)
    {
        var cut = RenderComponent<UiInputFile>(p =>
        {
            p.Add(c => c.Variant, variant);
            if (label != null) p.Add(c => c.Label, label);
            if (buttonText != null) p.Add(c => c.ButtonText, buttonText);
        });

        Assert.Equal(expected, cut.Find("input[type=file]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Le_bouton_de_fermeture_d_une_alerte_est_nomme_en_francais_et_personnalisable()
    {
        var alert = RenderComponent<UiAlert>(p => p.Add(c => c.Dismissible, true).Add(c => c.Message, "x"));
        Assert.Equal("Fermer", alert.Find(".ui-alert-close").GetAttribute("aria-label"));

        var custom = RenderComponent<UiAlert>(p => p.Add(c => c.Dismissible, true).Add(c => c.CloseLabel, "Dismiss").Add(c => c.Message, "x"));
        Assert.Equal("Dismiss", custom.Find(".ui-alert-close").GetAttribute("aria-label"));
    }
}
