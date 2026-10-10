namespace WaesesUI;

/// <summary>
/// Valeur en cascade fournie au composant affiché dans un dialogue :
/// <c>[CascadingParameter] UiDialogInstance Dialog { get; set; }</c>.
/// </summary>
public sealed class UiDialogInstance
{
    private readonly UiDialogReference _reference;
    private readonly Action _titleChanged;
    private string? _title;

    internal UiDialogInstance(UiDialogReference reference, string? title, Action titleChanged)
    {
        _reference = reference;
        _title = title;
        _titleChanged = titleChanged;
    }

    public Guid Id => _reference.Id;

    public string? Title
    {
        get => _title;
        set
        {
            if (_title == value) return;
            _title = value;
            _titleChanged();
        }
    }

    public void Close(object? data = null) => _reference.Close(data);
    public void Cancel() => _reference.Cancel();
}
