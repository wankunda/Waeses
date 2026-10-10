namespace WaesesUI.Tests;

public sealed class MessageBoxServiceTests
{
    public static IEnumerable<object[]> Types() => Enum.GetValues<MessageBoxType>().Select(t => new object[] { t });

    private static (MessageBoxOptions Options, Task<bool> Result) Show(Func<MessageBoxService, Task<bool>> call)
    {
        var service = new MessageBoxService();
        MessageBoxOptions? captured = null;
        service.OnShow += (options, tcs) => { captured = options; tcs.SetResult(true); };
        var result = call(service);
        return (captured!, result);
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void Une_confirmation_propose_toujours_d_annuler_quel_que_soit_son_type(MessageBoxType type)
    {
        var (options, _) = Show(s => s.ShowAsync("Supprimer ?", "Définitif.", type));

        Assert.True(options.ShowCancel, $"Pas de bouton Annuler pour une confirmation de type {type}.");
        Assert.Equal(type, options.Type);
    }

    [Theory]
    [MemberData(nameof(Types))]
    public void Une_alerte_n_a_jamais_de_bouton_annuler(MessageBoxType type)
    {
        var (options, _) = Show(s => s.ShowAlertAsync("Info", "Fait.", type));

        Assert.False(options.ShowCancel);
        Assert.Equal(string.Empty, options.CancelText);
    }

    [Fact]
    public void Les_libelles_par_defaut_suivent_UiOptions()
    {
        var service = new MessageBoxService(new UiOptions { ConfirmText = "Valider", CancelText = "Retour", OkText = "Compris" });
        MessageBoxOptions? confirm = null, alert = null;
        service.OnShow += (options, tcs) => { if (options.ShowCancel) confirm = options; else alert = options; tcs.SetResult(true); };

        _ = service.ShowAsync("t", "m");
        _ = service.ShowAlertAsync("t", "m");

        Assert.Equal(("Valider", "Retour"), (confirm!.ConfirmText, confirm.CancelText));
        Assert.Equal("Compris", alert!.ConfirmText);
    }
}
