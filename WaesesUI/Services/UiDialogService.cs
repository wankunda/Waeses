using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>Poignée sur un dialogue ouvert (équivalent de <c>IDialogReference</c> de MudBlazor).</summary>
public interface IUiDialogReference
{
    Guid Id { get; }

    /// <summary>Se termine à la fermeture du dialogue, avec son résultat.</summary>
    Task<UiDialogResult> Result { get; }

    /// <summary>Instance du composant affiché, disponible après son premier rendu.</summary>
    object? Dialog { get; }

    /// <summary>Ferme le dialogue avec une donnée (résultat non annulé).</summary>
    void Close(object? data = null);

    /// <summary>Ferme le dialogue avec le résultat donné.</summary>
    void Close(UiDialogResult? result);

    /// <summary>Annule le dialogue.</summary>
    void Cancel();

    /// <summary>Attend la fermeture et renvoie la donnée typée (<c>default</c> si le dialogue est annulé).</summary>
    Task<T?> GetReturnValueAsync<T>();
}

public interface IUiDialogService
{
    /// <summary>Levé quand un dialogue doit être affiché (écouté par <see cref="UiDialogProvider"/>).</summary>
    event Action<UiDialogInstanceInfo>? OnShow;

    /// <summary>Affiche <typeparamref name="TComponent"/> dans un dialogue ; attendre <c>Result</c> pour sa fermeture.</summary>
    Task<IUiDialogReference> ShowAsync<TComponent>(string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null)
        where TComponent : IComponent;

    /// <summary>Affiche le composant <paramref name="componentType"/> dans un dialogue.</summary>
    Task<IUiDialogReference> ShowAsync(Type componentType, string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null);

    /// <summary>
    /// Boîte de message à trois réponses (équivalent de <c>ShowMessageBox</c> de MudBlazor) :
    /// <c>true</c> pour <paramref name="yesText"/>, <c>false</c> pour <paramref name="noText"/>, <c>null</c> pour <paramref name="cancelText"/> ou une fermeture.
    /// Un texte <c>null</c> masque le bouton correspondant ; <paramref name="yesText"/> vaut <see cref="UiOptions.OkText"/> par défaut.
    /// </summary>
    Task<bool?> ShowMessageBoxAsync(string? title, string message, string? yesText = null, string? noText = null, string? cancelText = null, UiDialogOptions? options = null);

    /// <summary>Ferme le dialogue <paramref name="reference"/> avec <paramref name="result"/> (non annulé par défaut).</summary>
    void Close(IUiDialogReference reference, UiDialogResult? result = null);

    /// <summary>Annule tous les dialogues ouverts.</summary>
    void CancelAll();
}

/// <summary>Données transmises au provider pour un dialogue à afficher.</summary>
public sealed class UiDialogInstanceInfo
{
    public required UiDialogReference Reference { get; init; }
    public required Type ComponentType { get; init; }
    public string? Title { get; init; }
    public UiDialogParameters Parameters { get; init; } = new();
    public UiDialogOptions Options { get; init; } = new();
}

public sealed class UiDialogReference : IUiDialogReference
{
    private readonly TaskCompletionSource<UiDialogResult> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Guid Id { get; } = Guid.NewGuid();
    public Task<UiDialogResult> Result => _tcs.Task;
    public object? Dialog { get; internal set; }

    /// <summary>Levé quand le dialogue demande sa fermeture (écouté par le provider).</summary>
    public event Action? CloseRequested;

    public void Close(object? data = null) => Complete(UiDialogResult.Ok(data));
    public void Close(UiDialogResult? result) => Complete(result ?? UiDialogResult.Ok());
    public void Cancel() => Complete(UiDialogResult.Cancel());

    public async Task<T?> GetReturnValueAsync<T>() => (await Result).GetData<T>();

    private void Complete(UiDialogResult result)
    {
        if (_tcs.TrySetResult(result))
        {
            CloseRequested?.Invoke();
        }
    }
}

public class UiDialogService : IUiDialogService
{
    private readonly UiOptions _options;
    private readonly List<UiDialogReference> _open = new();

    public UiDialogService(UiOptions? options = null) => _options = options ?? new UiOptions();

    public event Action<UiDialogInstanceInfo>? OnShow;

    public Task<IUiDialogReference> ShowAsync<TComponent>(string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null)
        where TComponent : IComponent
        => ShowAsync(typeof(TComponent), title, parameters, options);

    public Task<IUiDialogReference> ShowAsync(Type componentType, string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(componentType);
        if (!typeof(IComponent).IsAssignableFrom(componentType))
        {
            throw new ArgumentException($"{componentType.Name} n'est pas un composant Blazor.", nameof(componentType));
        }

        if (OnShow is null)
        {
            throw new InvalidOperationException("Aucun <UiDialogProvider /> n'est rendu. Ajoutez-le une fois dans le layout.");
        }

        var reference = new UiDialogReference();
        _open.Add(reference);
        reference.CloseRequested += () => _open.Remove(reference);

        OnShow.Invoke(new UiDialogInstanceInfo
        {
            Reference = reference,
            ComponentType = componentType,
            Title = title,
            Parameters = parameters ?? new UiDialogParameters(),
            Options = options ?? new UiDialogOptions()
        });
        return Task.FromResult<IUiDialogReference>(reference);
    }

    public async Task<bool?> ShowMessageBoxAsync(string? title, string message, string? yesText = null, string? noText = null, string? cancelText = null, UiDialogOptions? options = null)
    {
        var parameters = new UiDialogParameters<UiMessageBoxDialog>()
            .Add(x => x.Message, message)
            .Add(x => x.YesText, yesText ?? _options.OkText)
            .Add(x => x.NoText, noText)
            .Add(x => x.CancelText, cancelText);

        var reference = await ShowAsync<UiMessageBoxDialog>(title, parameters, options ?? new UiDialogOptions { Size = UiSize.Sm });
        var result = await reference.Result;
        return result.Canceled ? null : result.GetData<bool>();
    }

    public void Close(IUiDialogReference reference, UiDialogResult? result = null) => reference.Close(result);

    public void CancelAll()
    {
        foreach (var reference in _open.ToList())
        {
            reference.Cancel();
        }
    }
}
