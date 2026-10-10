using System.Globalization;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace WaesesUI.Tests;

public sealed class SelectOptionTests : TestContext
{
    private enum Level
    {
        [System.ComponentModel.Description("Niveau bas")] Low,
        High
    }

    [Flags]
    private enum Rights { None = 0, Read = 1, Write = 2 }

    public SelectOptionTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddWaesesUIServices();
    }

    private static RenderFragment Options<T>(params (T Value, string? Text, bool Disabled)[] options) => b =>
    {
        var seq = 0;
        foreach (var (value, text, disabled) in options)
        {
            b.OpenComponent<UiSelectOption<T>>(seq++);
            b.AddAttribute(seq++, "Value", value);
            if (text != null) b.AddAttribute(seq++, "Text", text);
            b.AddAttribute(seq++, "Disabled", disabled);
            b.CloseComponent();
        }
    };

    private IRenderedComponent<UiSelect<T>> RenderSelect<T>(T initial, RenderFragment options, Action<T>? onChange = null)
    {
        var current = initial;
        return RenderComponent<UiSelect<T>>(p => p
            .Add(c => c.Value, initial)
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<T>(this, v => { current = v; onChange?.Invoke(v); }))
            .Add(c => c.ValueExpression, () => current)
            .Add(c => c.ChildContent, options));
    }

    [Fact]
    public void L_option_rend_valeur_libelle_et_etat_desactive()
    {
        var cut = RenderSelect("b", Options(("a", "Alpha", false), ("b", "Beta", false), ("c", "Gamma", true)));

        var options = cut.FindAll("select option");
        Assert.Equal(new[] { "a", "b", "c" }, options.Select(o => o.GetAttribute("value")).ToArray());
        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, options.Select(o => o.TextContent).ToArray());
        Assert.False(options[0].HasAttribute("disabled"));
        Assert.True(options[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Choisir_une_option_met_a_jour_la_valeur_liee()
    {
        string? chosen = null;
        var cut = RenderSelect<string?>("a", Options(("a", "Alpha", false), ("b", "Beta", false)), v => chosen = v);

        cut.Find("select").Change("b");

        Assert.Equal("b", chosen);
    }

    [Fact]
    public void Sans_libelle_l_option_affiche_sa_valeur_et_le_contenu_prime_sur_Text()
    {
        var cut = RenderComponent<UiSelectOption<string>>(p => p.Add(c => c.Value, "x"));
        Assert.Equal("x", cut.Find("option").TextContent);

        var withContent = RenderComponent<UiSelectOption<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Text, "texte")
            .Add(c => c.ChildContent, b => b.AddContent(0, "contenu")));
        Assert.Equal("contenu", withContent.Find("option").TextContent);
    }

    [Fact]
    public void Les_decimaux_sont_formates_en_culture_invariante_et_se_relisent_en_culture_francaise()
    {
        decimal? chosen = null;
        var cut = RenderSelect(1.5m, Options((1.5m, "Un et demi", false), (2.25m, "Deux un quart", false)), v => chosen = v);

        Assert.Equal(new[] { "1.5", "2.25" }, cut.FindAll("option").Select(o => o.GetAttribute("value")).ToArray());

        cut.Find("select").Change("2.25");

        Assert.Equal(2.25m, chosen);
    }

    [Fact]
    public void Enums_guid_et_nullables_sont_pris_en_charge()
    {
        Level? level = null;
        var enumSelect = RenderSelect(Level.Low, Options((Level.Low, "Bas", false), (Level.High, "Haut", false)), _ => { });
        Assert.Equal(new[] { "Low", "High" }, enumSelect.FindAll("option").Select(o => o.GetAttribute("value")).ToArray());
        enumSelect.Find("select").Change("High");

        var id = Guid.NewGuid();
        Guid? chosen = id;
        var nullable = RenderSelect<Guid?>(id, Options<Guid?>((null, "—", false), (id, "A", false)), v => chosen = v);
        Assert.Equal(string.Empty, nullable.FindAll("option")[0].GetAttribute("value"));
        Assert.Equal(id.ToString(), nullable.FindAll("option")[1].GetAttribute("value"));

        nullable.Find("select").Change("");
        Assert.Null(chosen);
        Assert.Null(level);
    }

    [Fact]
    public void Les_options_natives_continuent_de_fonctionner_et_peuvent_etre_melangees()
    {
        string? chosen = null;
        var current = "a";
        var cut = RenderComponent<UiSelect<string?>>(p => p
            .Add(c => c.Value, "a")
            .Add(c => c.ValueChanged, EventCallback.Factory.Create<string?>(this, v => chosen = v))
            .Add(c => c.ValueExpression, () => current)
            .Add(c => c.ChildContent, b =>
            {
                b.AddMarkupContent(0, "<option value=\"native\">Natif</option>");
                b.OpenComponent<UiSelectOption<string>>(1);
                b.AddAttribute(2, "Value", "comp");
                b.AddAttribute(3, "Text", "Composant");
                b.CloseComponent();
            }));

        Assert.Equal(new[] { "native", "comp" }, cut.FindAll("option").Select(o => o.GetAttribute("value")).ToArray());
        cut.Find("select").Change("comp");
        Assert.Equal("comp", chosen);
    }

    [Fact]
    public void L_option_hors_select_reste_utilisable_avec_le_meme_format()
    {
        var cut = RenderComponent<UiSelectOption<decimal>>(p => p.Add(c => c.Value, 3.5m).Add(c => c.Text, "t"));
        Assert.Equal("3.5", cut.Find("option").GetAttribute("value"));
    }

    [Fact]
    public void Class_Style_et_attributs_libres_sont_transmis_a_l_option()
    {
        var cut = RenderComponent<UiSelectOption<string>>(p => p
            .Add(c => c.Value, "x")
            .Add(c => c.Class, "extra")
            .Add(c => c.Style, "color:red")
            .AddUnmatched("data-k", "v"));

        var option = cut.Find("option");
        Assert.Equal("extra", option.ClassName);
        Assert.Equal("color:red", option.GetAttribute("style"));
        Assert.Equal("v", option.GetAttribute("data-k"));
    }

    // ---------- Mode Items ----------
    private IRenderedComponent<UiSelect<T>> RenderItems<T>(T initial, IEnumerable<T> items, Action<ComponentParameterCollectionBuilder<UiSelect<T>>>? extra = null, Action<T>? onChange = null)
    {
        var current = initial;
        return RenderComponent<UiSelect<T>>(p =>
        {
            p.Add(c => c.Value, initial);
            p.Add(c => c.ValueChanged, EventCallback.Factory.Create<T>(this, v => { current = v; onChange?.Invoke(v); }));
            p.Add(c => c.ValueExpression, () => current);
            p.Add(c => c.Items, items);
            extra?.Invoke(p);
        });
    }

    private static string[] Values<T>(IRenderedComponent<UiSelect<T>> cut) => cut.FindAll("select option").Select(o => o.GetAttribute("value")!).ToArray();
    private static string[] Texts<T>(IRenderedComponent<UiSelect<T>> cut) => cut.FindAll("select option").Select(o => o.TextContent).ToArray();

    [Fact]
    public void Items_genere_une_option_par_element_avec_le_texte_par_defaut()
    {
        string? chosen = null;
        var cut = RenderItems<string?>("b", new[] { "a", "b", "c" }, onChange: v => chosen = v);

        Assert.Equal(new[] { "a", "b", "c" }, Values(cut));
        Assert.Equal(new[] { "a", "b", "c" }, Texts(cut));

        cut.Find("select").Change("c");
        Assert.Equal("c", chosen);
    }

    [Fact]
    public void Items_enum_utilise_Description_quand_elle_existe()
    {
        Level chosen = Level.Low;
        var cut = RenderItems(Level.Low, Enum.GetValues<Level>(), onChange: v => chosen = v);

        Assert.Equal(new[] { "Low", "High" }, Values(cut));
        Assert.Equal(new[] { "Niveau bas", "High" }, Texts(cut));

        cut.Find("select").Change("High");
        Assert.Equal(Level.High, chosen);
    }

    [Fact]
    public void Items_enum_combine_sans_champ_nomme_ne_plante_pas()
    {
        var cut = RenderItems(Rights.None, new[] { Rights.None, Rights.Read, Rights.Read | Rights.Write });

        Assert.Equal(new[] { "None", "Read", "Read, Write" }, Texts(cut));
    }

    [Fact]
    public void ItemText_et_ItemDisabled_personnalisent_les_options()
    {
        var cut = RenderItems(2, new[] { 1, 2, 3 }, p => p
            .Add(c => c.ItemText, n => $"Option {n}")
            .Add(c => c.ItemDisabled, n => n == 3));

        Assert.Equal(new[] { "Option 1", "Option 2", "Option 3" }, Texts(cut));
        var options = cut.FindAll("select option");
        Assert.False(options[0].HasAttribute("disabled"));
        Assert.True(options[2].HasAttribute("disabled"));
    }

    [Fact]
    public void Items_decimaux_en_culture_invariante_et_element_null_pour_les_nullables()
    {
        decimal? chosen = null;
        var cut = RenderItems<decimal?>(1.5m, new decimal?[] { null, 1.5m, 2.25m }, onChange: v => chosen = v);

        Assert.Equal(new[] { "", "1.5", "2.25" }, Values(cut));
        Assert.Equal("", Texts(cut)[0]);

        cut.Find("select").Change("2.25");
        Assert.Equal(2.25m, chosen);
        cut.Find("select").Change("");
        Assert.Null(chosen);
    }

    [Fact]
    public void Items_precede_ChildContent_et_suit_le_Placeholder()
    {
        var cut = RenderItems<string?>("a", new[] { "a", "b" }, p => p
            .Add(c => c.Placeholder, "Choisir")
            .Add(c => c.ChildContent, Options<string?>(("z", "Zèbre", false))));

        Assert.Equal(new[] { "", "a", "b", "z" }, Values(cut));
        Assert.Equal(new[] { "Choisir", "a", "b", "Zèbre" }, Texts(cut));
    }

    [Fact]
    public void Items_se_met_a_jour_quand_la_liste_change()
    {
        var cut = RenderItems<string?>("a", new[] { "a" });
        Assert.Single(cut.FindAll("option"));

        cut.SetParametersAndRender(p => p.Add(c => c.Items, new[] { "a", "b", "c" }));

        Assert.Equal(new[] { "a", "b", "c" }, Values(cut));
    }
}
