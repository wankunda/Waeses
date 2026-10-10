namespace WaesesUI;

/// <summary>Valeurs par défaut globales, configurées via <c>AddWaesesUIServices(o =&gt; ...)</c>.</summary>
public class UiOptions
{
    public int ToastDurationMs { get; set; } = 4000;
    public ToastPosition ToastPosition { get; set; } = ToastPosition.BottomRight;
    public int MaxToasts { get; set; } = 5;
    public string ConfirmText { get; set; } = "Confirmer";
    public string CancelText { get; set; } = "Annuler";
    public string OkText { get; set; } = "OK";
    public string CloseLabel { get; set; } = "Fermer";

    /// <summary>Thème par défaut appliqué par <see cref="UiThemeProvider"/> (surchargé par son paramètre <c>Definition</c>).</summary>
    public UiThemeDefinition? Theme { get; set; }
}
