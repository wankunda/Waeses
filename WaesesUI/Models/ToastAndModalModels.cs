namespace WaesesUI;

public class ToastModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = string.Empty;
    public string? Title { get; set; }
    public ToastType Type { get; set; } = ToastType.Info;
    public int DurationMs { get; set; } = 4000;
    public bool ShowProgressBar { get; set; } = true;
    public bool PauseOnHover { get; set; } = true;
    /// <summary>Position propre à ce toast. Null : il suit celle de <c>UiToastContainer.Position</c>, puis <c>UiOptions.ToastPosition</c>.</summary>
    public ToastPosition? Position { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class MessageBoxOptions
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public MessageBoxType Type { get; set; } = MessageBoxType.Confirm;
    public string ConfirmText { get; set; } = "Confirmer";
    public string CancelText { get; set; } = "Annuler";
    public bool ShowCancel { get; set; } = true;
    public bool CloseOnEscape { get; set; } = true;
}
