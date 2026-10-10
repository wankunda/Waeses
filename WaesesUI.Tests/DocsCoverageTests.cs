using Microsoft.AspNetCore.Components;
using WaesesUIDocs.Docs;

namespace WaesesUI.Tests;

/// <summary>Porte de qualité : un composant présenté dans la documentation ne peut pas avoir de paramètre sans description.</summary>
public sealed class DocsCoverageTests
{
    public static IEnumerable<object[]> Components() => DocsCatalog.ApiComponents.Select(type => new object[] { type });

    [Theory]
    [MemberData(nameof(Components))]
    public void Chaque_parametre_d_un_composant_documente_a_une_description(Type component)
    {
        var missing = ApiDocs.Parameters(component, includeCommon: true)
            .Where(p => string.IsNullOrWhiteSpace(p.Summary))
            .Select(p => p.Name)
            .ToList();

        Assert.True(missing.Count == 0, $"{ApiDocs.DisplayName(component)} : paramètres sans description : {string.Join(", ", missing)}");
    }

    private static Type Definition(Type type) => type.IsGenericType ? type.GetGenericTypeDefinition() : type;

    private static IEnumerable<Type> PublicComponents() => typeof(UiButton).Assembly.GetTypes()
        .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic);

    /// <summary>
    /// Complétude : un composant public ajouté à la bibliothèque doit être documenté (<see cref="DocsCatalog.ApiComponents"/>)
    /// ou explicitement exclu avec sa raison (<see cref="DocsCatalog.ExcludedComponents"/>). Un oubli fait échouer ce test.
    /// </summary>
    [Fact]
    public void Chaque_composant_public_est_documente_ou_exclu_avec_une_raison()
    {
        var documented = DocsCatalog.ApiComponents.Select(Definition).ToHashSet();
        var excluded = DocsCatalog.ExcludedComponents.Keys.Select(Definition).ToHashSet();

        var missing = PublicComponents().Select(Definition).Where(t => !documented.Contains(t) && !excluded.Contains(t)).Select(t => t.Name).OrderBy(n => n).ToList();

        Assert.True(missing.Count == 0, "Composants publics ni documentés ni exclus : " + string.Join(", ", missing));
    }

    [Fact]
    public void Un_composant_n_est_pas_a_la_fois_documente_et_exclu_et_chaque_exclusion_a_une_raison()
    {
        var documented = DocsCatalog.ApiComponents.Select(Definition).ToHashSet();

        Assert.Empty(DocsCatalog.ExcludedComponents.Keys.Select(Definition).Where(documented.Contains));
        Assert.All(DocsCatalog.ExcludedComponents, e => Assert.True(e.Value.Length > 20, $"{e.Key.Name} : raison d'exclusion trop courte."));
        Assert.Equal(DocsCatalog.ApiComponents.Count, DocsCatalog.ApiComponents.Select(Definition).Distinct().Count());
    }

    [Fact]
    public void Les_composants_documentes_et_exclus_existent_tous_dans_la_bibliotheque()
    {
        var existing = PublicComponents().Select(Definition).ToHashSet();

        Assert.All(DocsCatalog.ApiComponents.Concat(DocsCatalog.ExcludedComponents.Keys), t => Assert.Contains(Definition(t), existing));
    }
}
