namespace WaesesUI;

public interface IToastService
{
    event Action? OnChange;
    IReadOnlyList<ToastModel> Toasts { get; }
    void ShowToast(string message, ToastType type = ToastType.Info, string? title = null, int durationMs = 4000, ToastPosition position = ToastPosition.BottomRight, bool showProgressBar = true, bool pauseOnHover = true);
    void ShowToast(ToastModel toast);
    void RemoveToast(string id);
    void ClearAll();
}

public class ToastService : IToastService
{
    private readonly List<ToastModel> _toasts = new();

    public event Action? OnChange;

    public IReadOnlyList<ToastModel> Toasts => _toasts.AsReadOnly();

    public void ShowToast(string message, ToastType type = ToastType.Info, string? title = null, int durationMs = 4000, ToastPosition position = ToastPosition.BottomRight, bool showProgressBar = true, bool pauseOnHover = true)
    {
        var toast = new ToastModel
        {
            Message = message,
            Type = type,
            Title = title,
            DurationMs = durationMs,
            Position = position,
            ShowProgressBar = showProgressBar,
            PauseOnHover = pauseOnHover
        };

        ShowToast(toast);
    }

    public void ShowToast(ToastModel toast)
    {
        _toasts.Add(toast);
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
