using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class FormTests : TestContext
{
    private sealed class Person
    {
        [Required(ErrorMessage = "Nom requis")]
        public string? Name { get; set; }

        public string? City { get; set; }
    }

    public FormTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private RenderFragment<UiForm> Fields(Person model) => form => b =>
    {
        b.OpenComponent<UiInput<string?>>(0);
        b.AddAttribute(1, "Value", model.Name);
        b.AddAttribute(2, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.Name = v));
        b.AddAttribute(3, "ValueExpression", (Expression<Func<string?>>)(() => model.Name));
        b.AddAttribute(4, "Id", "name");
        b.CloseComponent();

        b.OpenComponent<UiInput<string?>>(5);
        b.AddAttribute(6, "Value", model.City);
        b.AddAttribute(7, "ValueChanged", EventCallback.Factory.Create<string?>(this, v => model.City = v));
        b.AddAttribute(8, "ValueExpression", (Expression<Func<string?>>)(() => model.City));
        b.AddAttribute(9, "Id", "city");
        b.AddAttribute(10, "Validation", (Func<string?, string?>)(v => v == "Gotham" ? "Ville interdite" : null));
        b.CloseComponent();

        b.OpenElement(11, "button");
        b.AddAttribute(12, "type", "submit");
        b.AddContent(13, form.IsSubmitting ? "envoi" : "envoyer");
        b.CloseElement();
    };

    private IRenderedComponent<UiForm> RenderForm(Person model, Action<ComponentParameterCollectionBuilder<UiForm>>? extra = null) =>
        RenderComponent<UiForm>(p =>
        {
            p.Add(c => c.Model, model);
            p.Add(c => c.ChildContent, Fields(model));
            extra?.Invoke(p);
        });

    [Fact]
    public async Task Validate_signale_les_erreurs_DataAnnotations_et_alimente_Errors_et_IsValid()
    {
        var model = new Person();
        var cut = RenderForm(model);

        var valid = await cut.InvokeAsync(() => cut.Instance.Validate());

        Assert.False(valid);
        Assert.Equal(new[] { "Nom requis" }, cut.Instance.Errors);
        Assert.False(cut.Instance.IsValid);
        cut.WaitForAssertion(() => Assert.Contains("Nom requis", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public async Task Corriger_le_champ_rend_le_formulaire_valide()
    {
        var model = new Person();
        var cut = RenderForm(model);
        await cut.InvokeAsync(() => cut.Instance.Validate());

        cut.Find("#name").Input("Alice");

        Assert.True(await cut.InvokeAsync(() => cut.Instance.Validate()));
        Assert.True(cut.Instance.IsValid);
        Assert.Empty(cut.Instance.Errors);
        Assert.True(cut.Instance.IsTouched);
    }

    [Fact]
    public async Task La_validation_personnalisee_d_un_champ_participe_au_formulaire()
    {
        var model = new Person { Name = "Alice", City = "Gotham" };
        var cut = RenderForm(model);

        Assert.False(await cut.InvokeAsync(() => cut.Instance.Validate()));
        Assert.Equal(new[] { "Ville interdite" }, cut.Instance.Errors);
    }

    [Fact]
    public async Task IsValidChanged_est_notifie_aux_changements_d_etat()
    {
        var states = new List<bool>();
        var model = new Person();
        var cut = RenderForm(model, p => p.Add(c => c.IsValidChanged, EventCallback.Factory.Create<bool>(this, states.Add)));

        await cut.InvokeAsync(() => cut.Instance.Validate());
        cut.Find("#name").Input("Alice");
        await cut.InvokeAsync(() => cut.Instance.Validate());

        cut.WaitForAssertion(() => Assert.Equal(new[] { false, true }, states));
    }

    [Fact]
    public void Soumission_invalide_appelle_OnInvalidSubmit_et_valide_OnValidSubmit()
    {
        var valid = 0;
        var invalid = 0;
        var model = new Person();
        var cut = RenderForm(model, p => p
            .Add(c => c.OnValidSubmit, EventCallback.Factory.Create<EditContext>(this, _ => valid++))
            .Add(c => c.OnInvalidSubmit, EventCallback.Factory.Create<EditContext>(this, _ => invalid++)));

        cut.Find("form").Submit();
        Assert.Equal((0, 1), (valid, invalid));

        cut.Find("#name").Input("Alice");
        cut.Find("form").Submit();
        Assert.Equal((1, 1), (valid, invalid));
    }

    [Fact]
    public async Task IsSubmitting_est_vrai_pendant_la_soumission_asynchrone()
    {
        var gate = new TaskCompletionSource();
        var model = new Person { Name = "Alice" };
        var cut = RenderForm(model, p => p.Add(c => c.OnValidSubmit, EventCallback.Factory.Create<EditContext>(this, () => gate.Task)));

        var submit = cut.InvokeAsync(() => cut.Instance.SubmitAsync());
        cut.WaitForAssertion(() => Assert.Equal("envoi", cut.Find("button").TextContent));
        Assert.True(cut.Instance.IsSubmitting);

        gate.SetResult();
        await submit;

        cut.WaitForAssertion(() => Assert.Equal("envoyer", cut.Find("button").TextContent));
        Assert.False(cut.Instance.IsSubmitting);
    }

    [Fact]
    public async Task ResetValidation_efface_les_erreurs_sans_toucher_aux_valeurs()
    {
        var model = new Person { City = "Gotham" };
        var cut = RenderForm(model);
        await cut.InvokeAsync(() => cut.Instance.Validate());
        Assert.False(cut.Instance.IsValid);

        await cut.InvokeAsync(() => cut.Instance.ResetValidation());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=alert]")));
        Assert.True(cut.Instance.IsValid);
        Assert.Empty(cut.Instance.Errors);
        Assert.False(cut.Instance.IsTouched);
        Assert.Equal("Gotham", model.City);
        Assert.False(await cut.InvokeAsync(() => cut.Instance.Validate()));   // la validation fonctionne encore
    }

    [Fact]
    public async Task Sans_DataAnnotations_seules_les_validations_des_champs_comptent()
    {
        var model = new Person();   // Name vide mais DataAnnotations désactivé
        var cut = RenderForm(model, p => p.Add(c => c.DataAnnotations, false));

        Assert.True(await cut.InvokeAsync(() => cut.Instance.Validate()));
    }
}
