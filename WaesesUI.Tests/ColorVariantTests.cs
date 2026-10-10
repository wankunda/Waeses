using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class ColorVariantTests : TestContext
{
    public ColorVariantTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private static string[] Classes(AngleSharp.Dom.IElement element) =>
        element.ClassName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

    // ---------- Boutons ----------
    [Fact]
    public void Bouton_sans_Color_ni_Appearance_garde_les_classes_historiques()
    {
        var cut = RenderComponent<UiButton>(p => p.Add(c => c.Text, "Go").Add(c => c.Variant, UiButtonVariant.Danger));
        var classes = Classes(cut.Find("button"));

        Assert.Contains("ui-btn-danger", classes);
        Assert.DoesNotContain("ui-btn-filled", classes);
        Assert.DoesNotContain(classes, c => c.StartsWith("ui-color-"));
    }

    [Theory]
    [InlineData(UiColor.Success, null, "ui-color-success", "ui-btn-filled")]
    [InlineData(null, UiVariant.Outlined, "ui-color-primary", "ui-btn-outlined")]
    [InlineData(UiColor.Warning, UiVariant.Text, "ui-color-warning", "ui-btn-text")]
    [InlineData(UiColor.Neutral, UiVariant.Filled, "ui-color-neutral", "ui-btn-filled")]
    public void Color_et_Appearance_se_combinent_et_remplacent_Variant(UiColor? color, UiVariant? appearance, string colorClass, string appearanceClass)
    {
        var cut = RenderComponent<UiButton>(p => p
            .Add(c => c.Text, "Go")
            .Add(c => c.Variant, UiButtonVariant.Danger)
            .Add(c => c.Color, color)
            .Add(c => c.Appearance, appearance));
        var classes = Classes(cut.Find("button"));

        Assert.Contains(colorClass, classes);
        Assert.Contains(appearanceClass, classes);
        Assert.DoesNotContain("ui-btn-danger", classes);
        Assert.Contains("ui-btn-md", classes);
    }

    [Fact]
    public void FullWidth_et_EndIcon()
    {
        var cut = RenderComponent<UiButton>(p => p
            .Add(c => c.Text, "Suivant")
            .Add(c => c.EndIcon, "→")
            .Add(c => c.Icon, "←")
            .Add(c => c.FullWidth, true));

        Assert.Contains("ui-btn-block", Classes(cut.Find("button")));
        var icons = cut.FindAll(".ui-btn-icon");
        Assert.Equal(new[] { "←", "→" }, icons.Select(i => i.TextContent).ToArray());
        Assert.Contains("ui-btn-end-icon", icons[1].ClassName);
        Assert.True(cut.Markup.IndexOf("Suivant", StringComparison.Ordinal) < cut.Markup.IndexOf("→", StringComparison.Ordinal));
    }

    [Fact]
    public void Lien_et_bouton_icone_acceptent_aussi_Color_et_Appearance()
    {
        var link = RenderComponent<UiButton>(p => p.Add(c => c.Text, "Lien").Add(c => c.Href, "/x").Add(c => c.Color, UiColor.Info));
        Assert.Contains("ui-color-info", Classes(link.Find("a")));

        var fab = RenderComponent<UiButtonIcon>(p => p.Add(c => c.Icon, "+").Add(c => c.Color, UiColor.Danger).Add(c => c.Appearance, UiVariant.Outlined));
        var classes = Classes(fab.Find("button"));
        Assert.Contains("ui-btn-fab", classes);
        Assert.Contains("ui-color-danger", classes);
        Assert.Contains("ui-btn-outlined", classes);
        Assert.DoesNotContain("ui-btn-primary", classes);

        var legacy = RenderComponent<UiButtonIcon>(p => p.Add(c => c.Icon, "+"));
        Assert.Contains("ui-btn-primary", Classes(legacy.Find("button")));
    }

    // ---------- Champs ----------
    private IRenderedComponent<UiInput<string?>> RenderInput(UiVariant? variant = null, UiColor? color = null)
    {
        string? value = null;
        return RenderComponent<UiInput<string?>>(p => p
            .Add(c => c.ValueExpression, () => value)
            .Add(c => c.Variant, variant)
            .Add(c => c.Color, color)
            .Add(c => c.Class, "extra"));
    }

    [Fact]
    public void Champ_par_defaut_et_Outlined_n_ajoutent_aucune_classe_d_apparence()
    {
        foreach (var cut in new[] { RenderInput(), RenderInput(UiVariant.Outlined) })
        {
            var classes = Classes(cut.Find(".ui-form-group"));
            Assert.DoesNotContain("ui-field-filled", classes);
            Assert.DoesNotContain("ui-field-text", classes);
            Assert.Contains("extra", classes);
        }
    }

    [Fact]
    public void Champ_Filled_Text_et_Color()
    {
        Assert.Contains("ui-field-filled", Classes(RenderInput(UiVariant.Filled).Find(".ui-form-group")));
        Assert.Contains("ui-field-text", Classes(RenderInput(UiVariant.Text).Find(".ui-form-group")));

        var colored = Classes(RenderInput(UiVariant.Filled, UiColor.Danger).Find(".ui-form-group"));
        Assert.Contains("ui-field-filled", colored);
        Assert.Contains("ui-color-danger", colored);
        Assert.Contains("extra", colored);
    }

    [Fact]
    public void Variant_et_Color_s_appliquent_aux_autres_champs()
    {
        string? text = null;
        DateTime? date = null;
        var area = RenderComponent<UiTextArea<string?>>(p => p.Add(c => c.ValueExpression, () => text).Add(c => c.Variant, UiVariant.Text));
        Assert.Contains("ui-field-text", Classes(area.Find(".ui-form-group")));

        var picker = RenderComponent<UiDatePicker>(p => p.Add(c => c.ValueExpression, () => date).Add(c => c.Color, UiColor.Success));
        Assert.Contains("ui-color-success", Classes(picker.Find(".ui-form-group")));

        var select = RenderComponent<UiSelect<string?>>(p => p.Add(c => c.ValueExpression, () => text).Add(c => c.Variant, UiVariant.Filled));
        Assert.Contains("ui-field-filled", Classes(select.Find(".ui-form-group")));
    }

    [Fact]
    public void Combobox_transmet_l_apparence_a_son_conteneur()
    {
        var current = 0;
        var cut = RenderComponent<UiCombobox<string, int>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.DisplaySelector, s => s)
            .Add(c => c.ValueSelector, s => 1)
            .Add(c => c.ValueExpression, () => current)
            .Add(c => c.Variant, UiVariant.Filled)
            .Add(c => c.Color, UiColor.Info));

        var classes = Classes(cut.Find(".ui-form-group"));
        Assert.Contains("ui-field-filled", classes);
        Assert.Contains("ui-color-info", classes);
    }

    [Fact]
    public void ToCssClass_donne_un_nom_stable_par_couleur()
    {
        Assert.Equal("ui-color-primary", UiColor.Primary.ToCssClass());
        Assert.Equal("ui-color-neutral", UiColor.Neutral.ToCssClass());
    }
}
