namespace Observer.Desktop;

public enum MessageType
{
    Info,
    Warning,
    Error,
    Loading,
    LoadingComplete,
    Ask,
    Confirm
}

public class MessageService
{
    public event Func<string, MessageType, Task<bool>> Notify;

    public void AddHandler(Func<string, MessageType, Task<bool>> handler)
    {
        if (Notify == null)
            Notify += handler;
    }

    public async Task<bool> ShowLoading() => await Notify?.Invoke(string.Empty, MessageType.Loading);
    public async Task<bool> HideLoading() => await Notify?.Invoke(string.Empty, MessageType.LoadingComplete);
    public async Task<bool> Ask(string message) => await Notify?.Invoke(message, MessageType.Ask);
    public async Task<bool> Confirm(string message) => await Notify?.Invoke(message, MessageType.Confirm);
    public async Task<bool> Info(string message) => await Notify?.Invoke(message, MessageType.Info);
    public async Task<bool> Error(string message) => await Notify?.Invoke(message, MessageType.Error);
    public async Task<bool> Warn(string message) => await Notify?.Invoke(message, MessageType.Warning);
    public async Task<bool> ShowMessage(string message, MessageType messageType) => await Notify?.Invoke(message, messageType);
}
