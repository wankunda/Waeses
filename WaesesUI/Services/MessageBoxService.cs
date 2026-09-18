namespace WaesesUI;

public interface IMessageBoxService
{
    event Action<MessageBoxOptions, TaskCompletionSource<bool>>? OnShow;
    Task<bool> ShowAsync(string title, string message, MessageBoxType type = MessageBoxType.Confirm, string confirmText = "Confirmer", string cancelText = "Annuler");
    Task<bool> ShowAlertAsync(string title, string message, MessageBoxType type = MessageBoxType.Info, string confirmText = "OK");
}

public class MessageBoxService : IMessageBoxService
{
    public event Action<MessageBoxOptions, TaskCompletionSource<bool>>? OnShow;

    public Task<bool> ShowAsync(string title, string message, MessageBoxType type = MessageBoxType.Confirm, string confirmText = "Confirmer", string cancelText = "Annuler")
    {
        var tcs = new TaskCompletionSource<bool>();
        var options = new MessageBoxOptions
        {
            Title = title,
            Message = message,
            Type = type,
            ConfirmText = confirmText,
            CancelText = cancelText,
            ShowCancel = type == MessageBoxType.Confirm
        };

        OnShow?.Invoke(options, tcs);
        return tcs.Task;
    }

    public Task<bool> ShowAlertAsync(string title, string message, MessageBoxType type = MessageBoxType.Info, string confirmText = "OK")
    {
        var tcs = new TaskCompletionSource<bool>();
        var options = new MessageBoxOptions
        {
            Title = title,
            Message = message,
            Type = type,
            ConfirmText = confirmText,
            CancelText = string.Empty,
            ShowCancel = false
        };

        OnShow?.Invoke(options, tcs);
        return tcs.Task;
    }
}
