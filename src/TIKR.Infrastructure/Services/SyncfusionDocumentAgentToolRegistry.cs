using Microsoft.Extensions.AI;
using Syncfusion.AI.AgentTools.Core;
using Syncfusion.AI.AgentTools.DataExtraction;
using Syncfusion.AI.AgentTools.Excel;
using Syncfusion.AI.AgentTools.OfficeToPDF;
using Syncfusion.AI.AgentTools.PDF;
using Syncfusion.AI.AgentTools.PowerPoint;
using Syncfusion.AI.AgentTools.Word;
using SfAiTool = Syncfusion.AI.AgentTools.Core.AITool;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Syncfusion Document SDK Agent Tools in Storage Mode, exposed as <see cref="AIFunction"/> for Ollama tool calling.
/// See <see href="https://help.syncfusion.com/document-processing/ai-agent-tools/getting-started">Syncfusion Getting Started</see>.
/// </summary>
public sealed class SyncfusionDocumentAgentToolRegistry
{
    private readonly IReadOnlyList<AIFunction> _functions;

    public SyncfusionDocumentAgentToolRegistry(NasSyncfusionDocumentStorage storage)
    {
        var manager = new DocumentStorageManager(storage);
        var sfTools = BuildClerkDocumentTools(manager);

        _functions = sfTools
            .Select(t => AIFunctionFactory.Create(
                t.Method,
                t.Instance,
                new AIFunctionFactoryOptions { Name = t.Name, Description = t.Description }))
            .ToList();
    }

    public IReadOnlyList<AIFunction> GetFunctions() => _functions;

    /// <summary>
    /// Clerk-relevant Storage Mode tools across PDF, Word, Excel, PowerPoint, Office→PDF, and Smart Data Extraction.
    /// </summary>
    internal static IReadOnlyList<SfAiTool> BuildClerkDocumentTools(DocumentStorageManager manager)
    {
        var sfTools = new List<SfAiTool>();

        sfTools.AddRange(new PdfContentExtractionAgentTools(manager).GetTools());
        sfTools.AddRange(new PdfOperationsAgentTools(manager).GetTools());
        sfTools.AddRange(new WordImportExportAgentTools(manager).GetTools());
        sfTools.AddRange(new WordOperationsAgentTools(manager).GetTools());
        sfTools.AddRange(new ExcelWorksheetAgentTools(manager).GetTools());
        sfTools.AddRange(new PresentationContentAgentTools(manager).GetTools());
        sfTools.AddRange(new PresentationOperationsAgentTools(manager).GetTools());
        sfTools.AddRange(new OfficeToPdfAgentTools(manager).GetTools());
        sfTools.AddRange(new DataExtractionAgentTools(manager).GetTools());

        return sfTools;
    }
}
