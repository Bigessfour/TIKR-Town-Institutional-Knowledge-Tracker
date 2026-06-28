namespace TIKR.Web.Services;

public sealed record ClerkToastMessage(string Message, Func<Task>? UndoAsync);

public sealed class ClerkToastService
{
    public const int DefaultDurationMs = 5000;

    public event Action<ClerkToastMessage>? OnShow;

    public void Show(string message, Func<Task>? undoAsync = null) =>
        OnShow?.Invoke(new ClerkToastMessage(message, undoAsync));
}
