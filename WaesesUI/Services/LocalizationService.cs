using System.Globalization;

namespace WaesesUI;

public interface IUiLocalizationService
{
    CultureInfo Culture { get; }
    event Action? OnCultureChanged;
    string Get(string key, string fallback);
    string FormatNumber(double value, string? format = null);
    string FormatDate(DateTime value, string? format = null);
    Task SetCultureAsync(string cultureName);
}

public sealed class UiLocalizationService : IUiLocalizationService
{
    private static readonly IReadOnlyDictionary<string, string> French = new Dictionary<string, string>
    {
        ["ui.copy"] = "Copier", ["ui.copied"] = "Copié", ["ui.close"] = "Fermer",
        ["ui.cancel"] = "Annuler", ["ui.confirm"] = "Confirmer", ["ui.retry"] = "Réessayer",
        ["ui.loading"] = "Chargement...", ["ui.empty"] = "Aucun résultat"
    };

    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["ui.copy"] = "Copy", ["ui.copied"] = "Copied", ["ui.close"] = "Close",
        ["ui.cancel"] = "Cancel", ["ui.confirm"] = "Confirm", ["ui.retry"] = "Retry",
        ["ui.loading"] = "Loading...", ["ui.empty"] = "No results"
    };

    public CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo("fr-FR");
    public event Action? OnCultureChanged;

    public string Get(string key, string fallback)
    {
        var dictionary = Culture.TwoLetterISOLanguageName.Equals("en", StringComparison.OrdinalIgnoreCase) ? English : French;
        return dictionary.TryGetValue(key, out var value) ? value : fallback;
    }

    public string FormatNumber(double value, string? format = null) => value.ToString(format ?? "N", Culture);
    public string FormatDate(DateTime value, string? format = null) => value.ToString(format ?? "d", Culture);

    public Task SetCultureAsync(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        Culture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        OnCultureChanged?.Invoke();
        return Task.CompletedTask;
    }
}
