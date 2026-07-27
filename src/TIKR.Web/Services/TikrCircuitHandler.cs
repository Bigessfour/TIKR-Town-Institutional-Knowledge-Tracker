using Microsoft.AspNetCore.Components.Server.Circuits;

namespace TIKR.Web.Services;

/// <summary>
/// Logs Blazor Interactive Server circuit lifecycle and unhandled connection faults
/// so Documents / Vault click-through errors appear in Serilog files with context.
/// </summary>
public sealed class TikrCircuitHandler(ILogger<TikrCircuitHandler> logger) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit opened CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogInformation("Blazor circuit closed CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogDebug("Blazor circuit connection up CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        logger.LogWarning("Blazor circuit connection down CircuitId={CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
