namespace TIKR.Shared.DTOs;

/// <summary>Result of scanning a local forward-to-folder inbox into Documents.</summary>
public record EmailIngestionResult(int Ingested, int Skipped, IReadOnlyList<string> Errors);
