namespace WaesesUI;

/// <summary>Classes et styles partagés par <see cref="UiModal"/> et <see cref="UiDialogProvider"/>.</summary>
internal static class UiDialogCss
{
    public static string? SizeClass(UiSize size) => size switch
    {
        UiSize.Xs => "ui-modal-xs",
        UiSize.Sm => "ui-modal-sm",
        UiSize.Lg => "ui-modal-lg",
        UiSize.Xl => "ui-modal-xl",
        UiSize.Max => "ui-modal-max",
        _ => null
    };

    /// <summary>Largeur maximale ; aucune en plein écran (la classe <c>ui-modal-fullscreen</c> s'en charge).</summary>
    public static string? WidthStyle(UiSize size, bool fullScreen) => fullScreen ? null : size switch
    {
        UiSize.Xs => "max-width: 320px;",
        UiSize.Sm => "max-width: 400px;",
        UiSize.Lg => "max-width: 800px;",
        UiSize.Xl => "max-width: 1000px;",
        UiSize.Max => "max-width: 100%;",
        _ => "max-width: 540px;"
    };

    public static string? PositionClass(UiDialogPosition position) => position switch
    {
        UiDialogPosition.TopCenter => "ui-modal-pos-top",
        UiDialogPosition.TopLeft => "ui-modal-pos-top ui-modal-pos-left",
        UiDialogPosition.TopRight => "ui-modal-pos-top ui-modal-pos-right",
        UiDialogPosition.CenterLeft => "ui-modal-pos-left",
        UiDialogPosition.CenterRight => "ui-modal-pos-right",
        UiDialogPosition.BottomCenter => "ui-modal-pos-bottom",
        UiDialogPosition.BottomLeft => "ui-modal-pos-bottom ui-modal-pos-left",
        UiDialogPosition.BottomRight => "ui-modal-pos-bottom ui-modal-pos-right",
        _ => null
    };

    /// <summary>Options passées à <c>UiInterop.setupModal</c>.</summary>
    public static object JsOptions(UiDefaultFocus focus) => new
    {
        defaultFocus = focus switch
        {
            UiDefaultFocus.LastChild => "lastChild",
            UiDefaultFocus.Element => "element",
            UiDefaultFocus.None => "none",
            _ => "firstChild"
        }
    };

    public static string BackdropClass(UiDialogPosition position, bool fullScreen, string? extra) =>
        CssBuilder.Default("ui-modal-backdrop")
            .Add(PositionClass(position))
            .AddIf("ui-modal-backdrop-fullscreen", fullScreen)
            .Add(extra)
            .Build();

    public static string BoxClass(UiSize size, bool fullScreen, string? extra) =>
        CssBuilder.Default("ui-modal-box")
            .Add(SizeClass(size))
            .AddIf("ui-modal-fullscreen", fullScreen)
            .Add(extra)
            .Build();
}
