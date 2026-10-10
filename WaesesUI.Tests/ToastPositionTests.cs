using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

// UiToastContainer.Position définit la position par défaut ; un toast garde la sienne s'il en demande une.
public sealed class ToastPositionTests : TestContext
{
    public ToastPositionTests() => Services.AddWaesesUIServices(o => o.ToastPosition = ToastPosition.TopLeft);

    private IToastService Toasts => Services.GetRequiredService<IToastService>();

    private static IReadOnlyList<string> Classes(IRenderedComponent<UiToastContainer> cut) =>
        cut.FindAll(".ui-toast-container").Select(e => string.Join(' ', e.ClassList.Where(c => c != "ui-toast-container"))).ToList();

    [Fact]
    public void Sans_parametre_le_conteneur_suit_UiOptions()
    {
        var cut = RenderComponent<UiToastContainer>();

        Toasts.ShowToast("a");

        Assert.Equal(["ui-toast-topleft"], Classes(cut));
    }

    [Fact]
    public void La_position_du_conteneur_remplace_celle_de_UiOptions()
    {
        var cut = RenderComponent<UiToastContainer>(p => p.Add(c => c.Position, ToastPosition.BottomCenter));

        Toasts.ShowToast("a");

        Assert.Equal(["ui-toast-bottomcenter"], Classes(cut));
    }

    [Fact]
    public void Un_toast_avec_sa_propre_position_la_garde()
    {
        var cut = RenderComponent<UiToastContainer>(p => p.Add(c => c.Position, ToastPosition.BottomCenter));

        Toasts.ShowToast("par défaut");
        Toasts.ShowToast("à part", position: ToastPosition.TopRight);

        var classes = Classes(cut);
        Assert.Equal(2, classes.Count);
        Assert.Contains("ui-toast-bottomcenter", classes);
        Assert.Contains("ui-toast-topright", classes);
    }

    [Fact]
    public void Un_toast_construit_a_la_main_sans_position_suit_le_conteneur()
    {
        var cut = RenderComponent<UiToastContainer>(p => p.Add(c => c.Position, ToastPosition.TopCenter));

        Toasts.ShowToast(new ToastModel { Message = "a" });

        Assert.Null(Toasts.Toasts[0].Position);
        Assert.Equal(["ui-toast-topcenter"], Classes(cut));
    }

    [Fact]
    public void Changer_la_position_du_conteneur_deplace_les_toasts_sans_position()
    {
        var cut = RenderComponent<UiToastContainer>(p => p.Add(c => c.Position, ToastPosition.TopCenter));
        Toasts.ShowToast("a");
        Toasts.ShowToast("à part", position: ToastPosition.BottomLeft);

        cut.SetParametersAndRender(p => p.Add(c => c.Position, ToastPosition.BottomRight));

        var classes = Classes(cut);
        Assert.Contains("ui-toast-bottomright", classes);
        Assert.Contains("ui-toast-bottomleft", classes);
        Assert.DoesNotContain("ui-toast-topcenter", classes);
    }
}
