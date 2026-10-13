using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace WaesesUI;

/// <summary>
/// Composant affiché par le service pour un <see cref="UiDialog"/> en ligne : il redessine, à l'intérieur du dialogue,
/// le contenu déclaré dans la page (titre, contenu, actions), et le met à jour quand la page se redessine.
/// </summary>
internal sealed class UiInlineDialogHost : ComponentBase
{
    private UiDialogOptions? _appliedOptions;

    [CascadingParameter] private UiDialogInstance? Instance { get; set; }

    [Parameter] public UiDialog Source { get; set; } = default!;

    protected override void OnInitialized()
    {
        _appliedOptions = Source.Options;
        Source.AttachHost(this);
    }

    /// <summary>Appelé par le <see cref="UiDialog"/> en ligne à chaque rendu de la page.</summary>
    internal void Refresh()
    {
        if (Instance is not null && !ReferenceEquals(_appliedOptions, Source.Options))
        {
            _appliedOptions = Source.Options;
            Instance.SetOptions(Source.Options ?? new UiDialogOptions());
        }
        StateHasChanged();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<UiDialog>(0);
        builder.AddAttribute(1, nameof(UiDialog.Title), Source.Title);
        builder.AddAttribute(2, nameof(UiDialog.TitleContent), Source.TitleContent);
        builder.AddAttribute(3, nameof(UiDialog.DialogContent), Source.DialogContent);
        builder.AddAttribute(4, nameof(UiDialog.DialogActions), Source.DialogActions);
        builder.AddAttribute(5, nameof(UiDialog.OnBackdropClick), Source.OnBackdropClick);
        builder.AddAttribute(6, nameof(UiDialog.ContentClass), Source.ContentClass);
        builder.AddAttribute(7, nameof(UiDialog.ContentStyle), Source.ContentStyle);
        builder.AddAttribute(8, nameof(UiDialog.ActionsClass), Source.ActionsClass);
        builder.AddAttribute(9, nameof(UiDialog.TitleClass), Source.TitleClass);
        builder.AddAttribute(10, nameof(UiDialog.Gutters), Source.Gutters);
        builder.AddAttribute(11, nameof(UiDialog.Class), Source.Class);
        builder.AddAttribute(12, nameof(UiDialog.Style), Source.Style);
        builder.AddMultipleAttributes(13, Source.AdditionalAttributes);
        builder.CloseComponent();
    }
}
