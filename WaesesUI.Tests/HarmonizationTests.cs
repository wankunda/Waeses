using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class HarmonizationTests : TestContext
{
    public HarmonizationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private static string[] Classes(AngleSharp.Dom.IElement element) =>
        element.ClassName?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

    // ---------- UiStyle ----------
    [Fact]
    public void UiStyle_FieldClasses_et_ToneClasses()
    {
        Assert.Equal(string.Empty, UiStyle.FieldClasses(null, null));
        Assert.Equal(string.Empty, UiStyle.FieldClasses(UiVariant.Outlined, null));
        Assert.Equal("ui-field-filled ui-color-info", UiStyle.FieldClasses(UiVariant.Filled, UiColor.Info));
        Assert.Equal("ui-color-danger", UiStyle.FieldClasses(null, UiColor.Danger));

        Assert.Equal("ui-chip-warning", UiStyle.ToneClasses("ui-chip", UiColor.Warning, null));
        Assert.Equal("ui-color-warning ui-v-outlined", UiStyle.ToneClasses("ui-chip", UiColor.Warning, UiVariant.Outlined));
    }

    // ---------- Puces, badges, alertes, encadrés ----------
    [Fact]
    public void Chip_Badge_Alert_Callout_gardent_leur_rendu_historique_sans_Variant()
    {
        var chip = RenderComponent<UiChip>(p => p.Add(c => c.Text, "x").Add(c => c.Color, UiColor.Success));
        Assert.Contains("ui-chip-success", Classes(chip.Find(".ui-chip")));

        var badge = RenderComponent<UiBadge>(p => p.Add(c => c.Color, UiColor.Danger));
        Assert.Contains("ui-badge-danger", Classes(badge.Find(".ui-badge")));

        var alert = RenderComponent<UiAlert>(p => p.Add(c => c.Color, UiColor.Warning));
        Assert.Contains("ui-alert-warning", Classes(alert.Find(".ui-alert")));

        var callout = RenderComponent<UiCallout>(p => p.Add(c => c.Color, UiColor.Info));
        Assert.Contains("ui-callout-info", Classes(callout.Find(".ui-callout")));
    }

    [Theory]
    [InlineData(UiVariant.Filled, "ui-v-filled")]
    [InlineData(UiVariant.Outlined, "ui-v-outlined")]
    [InlineData(UiVariant.Text, "ui-v-text")]
    public void Variant_remplace_la_classe_historique_par_couleur_et_apparence(UiVariant variant, string expected)
    {
        var chip = Classes(RenderComponent<UiChip>(p => p.Add(c => c.Text, "x").Add(c => c.Color, UiColor.Info).Add(c => c.Variant, variant)).Find(".ui-chip"));
        var badge = Classes(RenderComponent<UiBadge>(p => p.Add(c => c.Color, UiColor.Info).Add(c => c.Variant, variant)).Find(".ui-badge"));
        var alert = Classes(RenderComponent<UiAlert>(p => p.Add(c => c.Color, UiColor.Info).Add(c => c.Variant, variant)).Find(".ui-alert"));
        var callout = Classes(RenderComponent<UiCallout>(p => p.Add(c => c.Color, UiColor.Info).Add(c => c.Variant, variant)).Find(".ui-callout"));

        foreach (var (classes, legacy) in new[] { (chip, "ui-chip-info"), (badge, "ui-badge-info"), (alert, "ui-alert-info"), (callout, "ui-callout-info") })
        {
            Assert.Contains(expected, classes);
            Assert.Contains("ui-color-info", classes);
            Assert.DoesNotContain(legacy, classes);
        }
    }

    // ---------- Progression et timeline ----------
    [Fact]
    public void Progress_et_timeline_posent_la_classe_de_couleur_commune()
    {
        var progress = RenderComponent<UiProgress>(p => p.Add(c => c.Value, 40).Add(c => c.Color, UiColor.Secondary));
        var fill = Classes(progress.Find(".ui-progress-fill"));
        Assert.Contains("ui-progress-fill-secondary", fill);
        Assert.Contains("ui-color-secondary", fill);

        var timeline = RenderComponent<UiTimeline>(p => p.Add(c => c.Items, new[]
        {
            new UiTimelineItem("A", Color: UiColor.Danger), new UiTimelineItem("B", Color: UiColor.Neutral)
        }));
        var markers = timeline.FindAll(".ui-timeline-marker");
        Assert.Contains("ui-color-danger", Classes(markers[0]));
        Assert.Contains("ui-color-neutral", Classes(markers[1]));
    }

    // ---------- Case, interrupteur, curseur, radio ----------
    [Fact]
    public void Checkbox_Switch_Slider_appliquent_Color_et_restent_neutres_sans_Color()
    {
        bool flag = false;
        double level = 0;

        var plain = RenderComponent<UiCheckbox>(p => p.Add(c => c.ValueExpression, () => flag));
        Assert.DoesNotContain(Classes(plain.Find(".ui-choice-item")), c => c.StartsWith("ui-color-"));

        var box = RenderComponent<UiCheckbox>(p => p.Add(c => c.ValueExpression, () => flag).Add(c => c.Color, UiColor.Success));
        Assert.Contains("ui-color-success", Classes(box.Find(".ui-choice-item")));

        var sw = RenderComponent<UiSwitch>(p => p.Add(c => c.ValueExpression, () => flag).Add(c => c.Color, UiColor.Danger));
        Assert.Contains("ui-color-danger", Classes(sw.Find(".ui-choice-item")));

        var slider = RenderComponent<UiSlider>(p => p.Add(c => c.ValueExpression, () => level).Add(c => c.Color, UiColor.Warning));
        Assert.Contains("ui-color-warning", Classes(slider.Find(".ui-slider-container")));
    }

    [Fact]
    public void Radio_herite_la_couleur_du_groupe_et_peut_la_surcharger()
    {
        var cut = RenderComponent<UiRadioGroup<string>>(p => p
            .Add(c => c.Color, UiColor.Success)
            .Add(c => c.ChildContent, b =>
            {
                b.OpenComponent<UiRadioOption<string>>(0);
                b.AddAttribute(1, "Value", "a");
                b.CloseComponent();
                b.OpenComponent<UiRadioOption<string>>(2);
                b.AddAttribute(3, "Value", "b");
                b.AddAttribute(4, "Color", UiColor.Danger);
                b.CloseComponent();
            }));

        var radios = cut.FindAll("[role=radio]");
        Assert.Contains("ui-color-success", Classes(radios[0]));
        Assert.Contains("ui-color-danger", Classes(radios[1]));
        Assert.DoesNotContain("ui-color-success", Classes(radios[1]));

        var none = RenderComponent<UiRadioGroup<string>>(p => p.Add(c => c.ChildContent, b =>
        {
            b.OpenComponent<UiRadioOption<string>>(0);
            b.AddAttribute(1, "Value", "a");
            b.CloseComponent();
        }));
        Assert.DoesNotContain(Classes(none.Find("[role=radio]")), c => c.StartsWith("ui-color-"));
    }

    // ---------- Champs composites ----------
    [Fact]
    public void TagInput_MultiSelect_et_DateRange_acceptent_Variant_et_Color()
    {
        var tags = RenderComponent<UiTagInput>(p => p.Add(c => c.Variant, UiVariant.Filled).Add(c => c.Color, UiColor.Info));
        var tagClasses = Classes(tags.Find(".ui-form-group"));
        Assert.Contains("ui-field-filled", tagClasses);
        Assert.Contains("ui-color-info", tagClasses);

        var range = RenderComponent<UiDateRangePicker>(p => p.Add(c => c.Variant, UiVariant.Text).Add(c => c.Color, UiColor.Danger));
        var rangeClasses = Classes(range.Find(".ui-form-group"));
        Assert.Contains("ui-field-text", rangeClasses);
        Assert.Contains("ui-color-danger", rangeClasses);

        var multi = RenderComponent<UiMultiSelect<string, string>>(p => p
            .Add(c => c.Items, new[] { "a" })
            .Add(c => c.DisplaySelector, s => s)
            .Add(c => c.ValueSelector, s => s)
            .Add(c => c.Variant, UiVariant.Filled));
        Assert.Contains("ui-field-filled", Classes(multi.Find(".ui-form-group")));
    }
}
