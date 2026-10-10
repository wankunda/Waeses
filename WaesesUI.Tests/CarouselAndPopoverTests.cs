using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class CarouselAndPopoverTests : TestContext
{
    private static readonly UiCarouselItem[] Items =
    {
        new("a.jpg", "A", "Légende A"),
        new("b.jpg", "B"),
        new("c.jpg", "C"),
    };

    public CarouselAndPopoverTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    // ---------- Carousel ----------
    private IRenderedComponent<UiCarousel> RenderCarousel(Action<ComponentParameterCollectionBuilder<UiCarousel>>? extra = null) =>
        RenderComponent<UiCarousel>(p => { p.Add(c => c.Items, Items); extra?.Invoke(p); });

    private static string ActiveLabel(IRenderedComponent<UiCarousel> cut) =>
        cut.Find(".ui-carousel-slide.active").GetAttribute("aria-label")!;

    [Fact]
    public void Carousel_rend_toutes_les_diapositives_et_marque_la_courante()
    {
        var cut = RenderCarousel();
        Assert.True(cut.FindAll(".ui-carousel-slide").Count == 3, cut.Markup);
        Assert.Equal("1 / 3", ActiveLabel(cut));
        Assert.Equal(2, cut.FindAll(".ui-carousel-slide[aria-hidden=true]").Count);
        Assert.Contains("translateX(-0%)", cut.Find(".ui-carousel-track").GetAttribute("style"));
    }

    [Fact]
    public void Carousel_suivant_avance_et_boucle()
    {
        var cut = RenderCarousel();
        cut.Find(".ui-carousel-control.next").Click();
        Assert.Equal("2 / 3", ActiveLabel(cut));
        cut.Find(".ui-carousel-control.next").Click();
        cut.Find(".ui-carousel-control.next").Click();
        Assert.Equal("1 / 3", ActiveLabel(cut));
    }

    [Fact]
    public void Carousel_sans_boucle_desactive_les_fleches_aux_extremites()
    {
        var cut = RenderCarousel(p => p.Add(c => c.Loop, false));
        Assert.True(cut.Find(".ui-carousel-control.previous").HasAttribute("disabled"));
        cut.Find(".ui-carousel-dots button:last-child").Click();
        Assert.True(cut.Find(".ui-carousel-control.next").HasAttribute("disabled"));
    }

    [Fact]
    public void Carousel_non_lie_conserve_son_index_quand_le_parent_re_rend()
    {
        var cut = RenderCarousel();
        cut.Find(".ui-carousel-control.next").Click();

        cut.SetParametersAndRender(p => p.Add(c => c.AriaLabel, "Autre"));

        Assert.Equal("2 / 3", ActiveLabel(cut));
    }

    [Fact]
    public void Carousel_suit_le_parametre_ActiveIndex_quand_il_change()
    {
        var cut = RenderCarousel(p => p.Add(c => c.ActiveIndex, 0));
        cut.SetParametersAndRender(p => p.Add(c => c.ActiveIndex, 2));
        Assert.Equal("3 / 3", ActiveLabel(cut));
    }

    [Fact]
    public void Carousel_notifie_ActiveIndexChanged()
    {
        var seen = -1;
        var cut = RenderCarousel(p => p.Add(c => c.ActiveIndexChanged, EventCallback.Factory.Create<int>(this, i => seen = i)));
        cut.Find(".ui-carousel-control.next").Click();
        Assert.Equal(1, seen);
    }

    [Fact]
    public void Carousel_navigue_au_clavier()
    {
        var cut = RenderCarousel();
        cut.Find("section").KeyDown(new KeyboardEventArgs { Key = "End" });
        Assert.Equal("3 / 3", ActiveLabel(cut));
        cut.Find("section").KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal("2 / 3", ActiveLabel(cut));
        cut.Find("section").KeyDown(new KeyboardEventArgs { Key = "Home" });
        Assert.Equal("1 / 3", ActiveLabel(cut));
    }

    [Fact]
    public void Carousel_glissement_tactile_change_de_diapositive()
    {
        var cut = RenderCarousel();
        cut.Find(".ui-carousel-stage").TouchStart(new TouchEventArgs { Touches = new[] { new TouchPoint { ClientX = 300 } } });
        cut.Find(".ui-carousel-stage").TouchEnd(new TouchEventArgs { ChangedTouches = new[] { new TouchPoint { ClientX = 100 } } });
        Assert.Equal("2 / 3", ActiveLabel(cut));
    }

    [Fact]
    public void Carousel_une_seule_image_sans_fleches_et_liste_vide_sans_erreur()
    {
        var single = RenderComponent<UiCarousel>(p => p.Add(c => c.Items, Items.Take(1).ToArray()));
        Assert.Empty(single.FindAll(".ui-carousel-control"));

        var empty = RenderComponent<UiCarousel>();
        Assert.Empty(empty.FindAll(".ui-carousel-slide"));
    }

    [Fact]
    public void Carousel_defilement_auto_coupe_la_region_live_et_utilise_ItemTemplate()
    {
        var cut = RenderCarousel(p => p
            .Add(c => c.AutoCycle, true)
            .Add(c => c.ItemTemplate, item => b => b.AddContent(0, $"custom-{item.Alt}")));
        Assert.Equal("off", cut.Find(".ui-carousel-stage").GetAttribute("aria-live"));
        Assert.Contains("custom-A", cut.Find(".ui-carousel-slide.active").TextContent);
        Assert.Empty(cut.FindAll("img"));
    }

    // ---------- Migration UiComponentBase ----------
    [Fact]
    public void Les_composants_migres_appliquent_Style_et_attributs_libres()
    {
        var card = RenderComponent<UiCard>(p => p.Add(c => c.Style, "margin:1px").AddUnmatched("data-k", "v"));
        Assert.Equal("margin:1px", card.Find("[data-k]").GetAttribute("style"));

        var badge = RenderComponent<UiBadge>(p => p.Add(c => c.Style, "color:red"));
        Assert.Contains("color:red", badge.Markup);
    }

    // ---------- Popover ----------
    [Fact]
    public void Popover_avec_provider_rend_son_contenu_dans_le_provider_et_pas_en_place()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var host = RenderComponent<UiPopover>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.Trigger, b => b.AddContent(0, "Ouvrir"))
            .Add(c => c.ChildContent, b => b.AddContent(0, "Contenu-popover")));

        provider.WaitForAssertion(() => Assert.Contains("Contenu-popover", provider.Find(".ui-popover-layer").TextContent));
        Assert.DoesNotContain("Contenu-popover", host.Markup);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.openPopover");
    }

    [Fact]
    public void Popover_sans_provider_reste_rendu_en_place()
    {
        var host = RenderComponent<UiPopover>(p => p
            .Add(c => c.IsOpen, true)
            .Add(c => c.ChildContent, b => b.AddContent(0, "Contenu-popover")));
        Assert.Contains("Contenu-popover", host.Find(".ui-popover-content").TextContent);
    }

    [Fact]
    public async Task Popover_se_ferme_via_la_demande_du_provider()
    {
        var provider = RenderComponent<UiPopoverProvider>();
        var closed = false;
        var panel = RenderComponent<UiPopoverPanel>(p => p
            .Add(c => c.Open, true)
            .Add(c => c.OpenChanged, EventCallback.Factory.Create<bool>(this, v => closed = !v))
            .Add(c => c.ChildContent, b => b.AddContent(0, "X")));
        provider.WaitForAssertion(() => provider.Find(".ui-popover-layer"));

        var entry = Services.GetRequiredService<IUiPopoverService>().Entries.Single();
        await panel.InvokeAsync(() => entry.RequestClose!());

        Assert.True(closed);
        provider.WaitForAssertion(() => Assert.Empty(provider.FindAll(".ui-popover-layer")));
        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "UiInterop.closePopover");
    }

    [Fact]
    public void Panel_retire_son_entree_a_la_destruction()
    {
        var service = Services.GetRequiredService<IUiPopoverService>();
        RenderComponent<UiPopoverPanel>(p => p.Add(c => c.Open, false));
        Assert.Single(service.Entries);
        DisposeComponents();
        Assert.Empty(service.Entries);
    }
}
