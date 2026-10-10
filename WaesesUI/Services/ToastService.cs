namespace WaesesUI;

public interface IToastService
{
    event Action? OnChange;
    IReadOnlyList<ToastModel> Toasts { get; }
    void ShowToast(string message, ToastType type = ToastType.Info, string? title = null, int? durationMs = null, ToastPosition? position = null, bool showProgressBar = true, bool pauseOnHover = true);
    void ShowToast(ToastModel toast);
    void RemoveToast(string id);
    void ClearAll();
}

public class ToastService : IToastService
{
    private readonly List<ToastModel> _toasts = new();
    private readonly UiOptions _options;

    public ToastService(UiOptions? options = null) => _options = options ?? new UiOptions();

    public event Action? OnChange;

    public IReadOnlyList<ToastModel> Toasts => _toasts.AsReadOnly();

    public void ShowToast(string message, ToastType type = ToastType.Info, string? title = null, int? durationMs = null, ToastPosition? position = null, bool showProgressBar = true, bool pauseOnHover = true)
    {
        var toast = new ToastModel
        {
            Message = message,
            Type = type,
            Title = title,
            DurationMs = durationMs ?? _options.ToastDurationMs,
            Position = position,   // null : la position par défaut est résolue à l'affichage (UiToastContainer, puis UiOptions)
            ShowProgressBar = showProgressBar,
            PauseOnHover = pauseOnHover
        };

        ShowToast(toast);
    }

    public void ShowToast(ToastModel toast)
    {
        _toasts.Add(toast);
        while (_options.MaxToasts > 0 && _toasts.Count > _options.MaxToasts)
        {
            _toasts.RemoveAt(0);
        }
        NotifyStateChanged();
    }

    public void RemoveToast(string id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast != null)
        {
            _toasts.Remove(toast);
            NotifyStateChanged();
        }
    }

    public void ClearAll()
    {
        _toasts.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
