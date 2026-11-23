namespace WaesesUI;

/// <summary>Ouvre une palette de commandes depuis n'importe quel composant de l'application.</summary>
public interface IUiCommandPaletteService
{
    /// <summary>Ouvre la palette enregistrée avec les commandes fournies.</summary>
    /// <exception cref="InvalidOperationException">Aucun composant <see cref="UiCommandPalette"/> n'écoute le service.</exception>
    Task OpenAsync(IReadOnlyList<UiCommand> commands);

    /// <summary>Événement interne de diffusion aux composants de palette actifs.</summary>
    event Func<IReadOnlyList<UiCommand>, Task>? OpenRequested;
}

internal sealed class UiCommandPaletteService : IUiCommandPaletteService
{
    public event Func<IReadOnlyList<UiCommand>, Task>? OpenRequested;

    public async Task OpenAsync(IReadOnlyList<UiCommand> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);

        var handlers = OpenRequested;
        if (handlers is null)
        {
            throw new InvalidOperationException("Aucun composant <UiCommandPalette /> n'est rendu pour écouter le service.");
        }

        var subscribers = handlers.GetInvocationList();
        if (subscribers.Length != 1)
        {
            throw new InvalidOperationException("Une seule instance de <UiCommandPalette /> peut écouter le service.");
        }

        var snapshot = commands.ToArray();
        await ((Func<IReadOnlyList<UiCommand>, Task>)subscribers[0])(snapshot);
    }
}
