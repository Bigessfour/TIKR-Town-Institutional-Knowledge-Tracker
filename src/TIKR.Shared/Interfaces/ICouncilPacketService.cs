using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Thin orchestration service for council packet generation (build requirements if needed, generate branded packet via Document SDK, persist with tx + audit).
/// Enforces separation: business logic here, endpoint remains thin delegation.
/// </summary>
public interface ICouncilPacketService
{
    Task<CouncilPacketResponse> GenerateCouncilPacketAsync(CreateCouncilPacketRequest? request, CancellationToken ct = default);
}
