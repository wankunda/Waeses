using Microsoft.JSInterop;

namespace WaesesUI;

internal static class JsRuntimeExtensions
{
    /// <summary>
    /// Appel JS de nettoyage (démontage) qui ne doit jamais faire échouer la libération d'un composant :
    /// circuit déjà fermé (<see cref="JSDisconnectedException"/>), prérendu statique (<see cref="InvalidOperationException"/> :
    /// « JavaScript interop calls cannot be issued at this time ») ou annulation.
    /// </summary>
    public static async ValueTask TryTeardownAsync(this IJSRuntime js, string identifier, params object?[] args)
    {
        try
        {
            await js.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException) { }
        catch (InvalidOperationException) { }
        catch (TaskCanceledException) { }
    }
}
