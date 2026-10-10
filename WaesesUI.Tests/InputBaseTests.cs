using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class InputBaseTests : TestContext
{
    private sealed class Model
    {
        public string? Name { get; set; }
        public bool Accept { get; set; }
    }

    public InputBaseTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private IRenderedComponent<UiInput<string?>> RenderInput(Model model, Action<ComponentParameterCollectionBuilder<UiInput<string?>>>? extra = null, EditContext? context = null, Action<string?>? onChange = null) =>
        RenderComponent<UiInput<string?>>(p =>
        {
            p.Add(c => c.Value, model.Name);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => { model.Name = v; onChange?.Invoke(v); }));
            p.Add(c => c.ValueExpression, () => model.Name);
            if (context != null) p.AddCascadingValue(context);
            extra?.Invoke(p);
        });

    // ---------- Identifiants ----------
    [Fact]
    public void Chaque_champ_recoit_un_id_avec_son_prefixe_et_un_id_explicite_est_conserve()
    {
        var model = new Model();
        Assert.StartsWith("input-", RenderInput(model).Find("input").Id);
        Assert.Equal("mon-id", RenderInput(model, p => p.Add(c => c.Id, "mon-id")).Find("input").Id);

        var select = RenderComponent<UiSelect<string?>>(p => p.Add(c => c.ValueExpression, () => model.Name));
        Assert.StartsWith("select-", select.Find("select").Id);
        var area = RenderComponent<UiTextArea<string?>>(p => p.Add(c => c.ValueExpression, () => model.Name));
        Assert.StartsWith("textarea-", area.Find("textarea").Id);
    }

    // ---------- ARIA ----------
    [Fact]
    public void Une_erreur_relie_le_champ_a_son_message_via_aria()
    {
        var cut = RenderInput(new Model(), p => p.Add(c => c.ErrorMessage, "Trop court").Add(c => c.HelperText, "Aide"));

        var input = cut.Find("input");
        var alert = cut.Find("[role=alert]");
        Assert.Equal("true", input.GetAttribute("aria-invalid"));
        Assert.Equal(alert.Id, input.GetAttribute("aria-describedby"));
        Assert.Contains("Trop court", alert.TextContent);
        Assert.Empty(cut.FindAll(".ui-helper-text"));
    }

    [Fact]
    public void Sans_erreur_le_texte_d_aide_decrit_le_champ()
    {
        var cut = RenderInput(new Model(), p => p.Add(c => c.HelperText, "Aide").Add(c => c.Required, true));

        var input = cut.Find("input");
        Assert.Null(input.GetAttribute("aria-invalid"));
        Assert.Equal(cut.Find(".ui-helper-text").Id, input.GetAttribute("aria-describedby"));
        Assert.Equal("true", input.GetAttribute("aria-required"));
    }

    [Fact]
    public void Le_drapeau_Error_force_l_etat_d_erreur_sans_message()
    {
        var cut = RenderInput(new Model(), p => p.Add(c => c.Error, true));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));
        Assert.Contains("ui-input-error", cut.Find("input").ClassName);
        Assert.Null(cut.Find("input").GetAttribute("aria-describedby"));
    }

    // ---------- Validation personnalisée ----------
    private static string? NotEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? "Nom requis" : null;

    [Fact]
    public async Task Validation_participe_a_la_validation_de_l_EditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var cut = RenderInput(model, p => p.Add(c => c.Validation, NotEmpty), context);

        Assert.False(await cut.InvokeAsync(() => context.Validate()));
        cut.WaitForAssertion(() => Assert.Contains("Nom requis", cut.Find("[role=alert]").TextContent));
        Assert.Equal("true", cut.Find("input").GetAttribute("aria-invalid"));

        cut.Find("input").Input("Alice");

        Assert.Equal("Alice", model.Name);
        Assert.True(await cut.InvokeAsync(() => context.Validate()));
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role=alert]")));
    }

    [Fact]
    public void Validation_hors_formulaire_ne_s_affiche_qu_apres_modification()
    {
        var model = new Model();
        var cut = RenderInput(model, p => p.Add(c => c.Validation, NotEmpty));
        Assert.Empty(cut.FindAll("[role=alert]"));

        cut.Find("input").Input("x");
        cut.SetParametersAndRender(p => p.Add(c => c.Value, "x"));      // le parent répercute la valeur liée
        cut.Find("input").Input("");
        cut.SetParametersAndRender(p => p.Add(c => c.Value, ""));

        cut.WaitForAssertion(() => Assert.Contains("Nom requis", cut.Find("[role=alert]").TextContent));
    }

    [Fact]
    public async Task Un_champ_detruit_se_desabonne_de_l_EditContext()
    {
        var model = new Model();
        var context = new EditContext(model);
        var calls = 0;
        var cut = RenderInput(model, p => p.Add(c => c.Validation, v => { calls++; return null; }), context);

        await cut.InvokeAsync(() => context.Validate());
        var before = calls;
        DisposeComponents();
        await Renderer.Dispatcher.InvokeAsync(() => context.Validate());

        Assert.True(before > 0);
        Assert.Equal(before, calls);
    }

    [Fact]
    public async Task Validation_s_applique_aussi_au_textarea_et_a_la_case_a_cocher()
    {
        var model = new Model();
        var context = new EditContext(model);

        var area = RenderComponent<UiTextArea<string?>>(p => p
            .Add(c => c.Value, model.Name)
            .Add(c => c.ValueExpression, () => model.Name)
            .Add(c => c.Validation, NotEmpty)
            .AddCascadingValue(context));
        var box = RenderComponent<UiCheckbox>(p => p
            .Add(c => c.Value, model.Accept)
            .Add(c => c.ValueExpression, () => model.Accept)
            .Add(c => c.Validation, v => v ? null : "À accepter")
            .AddCascadingValue(context));

        Assert.False(await area.InvokeAsync(() => context.Validate()));
        area.WaitForAssertion(() => Assert.Contains("Nom requis", area.Markup));
        box.WaitForAssertion(() => Assert.Contains("À accepter", box.Markup));
    }

    // ---------- Immediate / DebounceInterval ----------
    [Fact]
    public void Sans_Immediate_la_frappe_n_est_prise_en_compte_qu_a_la_validation_du_champ()
    {
        var model = new Model();
        var cut = RenderInput(model, p => p.Add(c => c.Immediate, false));

        cut.Find("input").Input("abc");
        Assert.Null(model.Name);

        cut.Find("input").Change("abc");
        Assert.Equal("abc", model.Name);
    }

    [Fact]
    public async Task DebounceInterval_attend_la_fin_de_la_frappe_et_ne_garde_que_la_derniere_valeur()
    {
        var model = new Model();
        var changes = new List<string?>();
        var cut = RenderInput(model, p => p.Add(c => c.DebounceInterval, 80), onChange: changes.Add);

        cut.Find("input").Input("a");
        cut.Find("input").Input("ab");
        Assert.Empty(changes);

        await Task.Delay(300);

        Assert.Equal(new[] { "ab" }, changes);
    }

    [Fact]
    public async Task Le_changement_de_valeur_annule_le_delai_en_cours()
    {
        var model = new Model();
        var changes = new List<string?>();
        var cut = RenderInput(model, p => p.Add(c => c.DebounceInterval, 150), onChange: changes.Add);

        cut.Find("input").Input("a");
        cut.Find("input").Change("final");
        await Task.Delay(350);

        Assert.Equal(new[] { "final" }, changes);
    }

    // ---------- Champs migrés ----------
    [Fact]
    public void Les_champs_migres_acceptent_ErrorMessage_Style_et_Class()
    {
        TimeSpan? time = null;
        double level = 0;
        bool on = false;
        var picker = RenderComponent<UiTimePicker>(p => p
            .Add(c => c.ValueExpression, () => time)
            .Add(c => c.ErrorMessage, "Heure invalide")
            .Add(c => c.Class, "extra")
            .Add(c => c.Style, "margin:1px"));
        Assert.Contains("Heure invalide", picker.Markup);

        var slider = RenderComponent<UiSlider>(p => p.Add(c => c.ValueExpression, () => level).Add(c => c.ErrorMessage, "Hors plage").Add(c => c.Style, "margin:2px"));
        Assert.Contains("Hors plage", slider.Markup);
        Assert.Contains("margin:2px", slider.Markup);

        var sw = RenderComponent<UiSwitch>(p => p.Add(c => c.ValueExpression, () => on).Add(c => c.ErrorMessage, "Requis"));
        Assert.Contains("Requis", sw.Markup);
    }
}
