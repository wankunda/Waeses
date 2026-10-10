namespace WaesesUI;

public interface IMessageBoxService
{
    event Action<MessageBoxOptions, TaskCompletionSource<bool>>? OnShow;
    Task<bool> ShowAsync(string title, string message, MessageBoxType type = MessageBoxType.Confirm, string? confirmText = null, string? cancelText = null);
    Task<bool> ShowAlertAsync(string title, string message, MessageBoxType type = MessageBoxType.Info, string? confirmText = null);
}

public class MessageBoxService : IMessageBoxService
{
    private readonly UiOptions _options;

    public MessageBoxService(UiOptions? options = null) => _options = options ?? new UiOptions();

    public event Action<MessageBoxOptions, TaskCompletionSource<bool>>? OnShow;

    public Task<bool> ShowAsync(string title, string message, MessageBoxType type = MessageBoxType.Confirm, string? confirmText = null, string? cancelText = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var options = new MessageBoxOptions
        {
            Title = title,
            Message = message,
            Type = type,
            ConfirmText = confirmText ?? _options.ConfirmText,
            CancelText = cancelText ?? _options.CancelText,
            ShowCancel = true
        };

        OnShow?.Invoke(options, tcs);
        return tcs.Task;
    }

    public Task<bool> ShowAlertAsync(string title, string message, MessageBoxType type = MessageBoxType.Info, string? confirmText = null)
    {
        var tcs = new TaskCompletionSource<bool>();
        var options = new MessageBoxOptions
        {
            Title = title,
            Message = message,
            Type = type,
            ConfirmText = confirmText ?? _options.OkText,
            CancelText = string.Empty,
            ShowCancel = false
        };

        OnShow?.Invoke(options, tcs);
        return tcs.Task;
    }
}
