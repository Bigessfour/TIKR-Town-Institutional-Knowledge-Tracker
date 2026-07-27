using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace TIKR.Web.Components.Shared;

/// <summary>
/// ErrorBoundary that always writes the caught exception to Serilog so Documents/Vault
/// click-through failures are greppable even when the UI only shows "Something went wrong".
/// </summary>
public sealed class TikrLoggingErrorBoundary : ErrorBoundary
{
    [Inject]
    private ILogger<TikrLoggingErrorBoundary> Logger { get; set; } = default!;

    protected override Task OnErrorAsync(Exception exception)
    {
        Logger.LogError(exception,
            "Blazor ErrorBoundary caught unhandled UI exception Type={ExceptionType} Message={Message}",
            exception.GetType().FullName,
            exception.Message);
        return base.OnErrorAsync(exception);
    }
}
