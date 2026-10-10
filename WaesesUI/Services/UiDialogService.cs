using Microsoft.AspNetCore.Components;

namespace WaesesUI;

/// <summary>Options d'affichage d'un dialogue.</summary>
public class UiDialogOptions
{
    public UiSize Size { get; set; } = UiSize.Md;
    public bool CloseOnBackdropClick { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
    public bool ShowCloseButton { get; set; } = true;
    public string? Class { get; set; }
}

/// <summary>Paramètres passés au composant affiché dans le dialogue.</summary>
public class UiDialogParameters : Dictionary<string, object?>
{
    public UiDialogParameters With(string name, object? value)
    {
        this[name] = value;
        return this;
    }
}

/// <summary>Résultat d'un dialogue : annulé, ou fermé avec une donnée optionnelle.</summary>
public sealed class UiDialogResult
{
    private UiDialogResult(bool canceled, object? data) { Canceled = canceled; Data = data; }

    public bool Canceled { get; }
    public object? Data { get; }

    public static UiDialogResult Ok(object? data = null) => new(false, data);
    public static UiDialogResult Cancel() => new(true, null);

    /// <summary>Donnée typée, ou <c>default</c> si le dialogue est annulé ou de type différent.</summary>
    public T? GetData<T>() => Data is T value ? value : default;
}

/// <summary>Poignée sur un dialogue ouvert.</summary>
public interface IUiDialogReference
{
    Guid Id { get; }
    Task<UiDialogResult> Result { get; }
    void Close(object? data = null);
    void Cancel();
}

public interface IUiDialogService
{
    /// <summary>Levé quand un dialogue doit être affiché (écouté par <see cref="UiDialogProvider"/>).</summary>
    event Action<UiDialogInstanceInfo>? OnShow;

    /// <summary>Affiche <typeparamref name="TComponent"/> dans un dialogue ; attendre <c>Result</c> pour sa fermeture.</summary>
    Task<IUiDialogReference> ShowAsync<TComponent>(string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null)
        where TComponent : IComponent;
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

    /// <summary>Levé quand le dialogue demande sa fermeture (écouté par le provider).</summary>
    public event Action? CloseRequested;

    public void Close(object? data = null) => Complete(UiDialogResult.Ok(data));
    public void Cancel() => Complete(UiDialogResult.Cancel());

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
    public event Action<UiDialogInstanceInfo>? OnShow;

    public Task<IUiDialogReference> ShowAsync<TComponent>(string? title = null, UiDialogParameters? parameters = null, UiDialogOptions? options = null)
        where TComponent : IComponent
    {
        if (OnShow is null)
        {
            throw new InvalidOperationException("Aucun <UiDialogProvider /> n'est rendu. Ajoutez-le une fois dans le layout.");
        }

        var reference = new UiDialogReference();
        OnShow.Invoke(new UiDialogInstanceInfo
        {
            Reference = reference,
            ComponentType = typeof(TComponent),
            Title = title,
            Parameters = parameters ?? new UiDialogParameters(),
            Options = options ?? new UiDialogOptions()
        });
        return Task.FromResult<IUiDialogReference>(reference);
    }
}
