using TIKR.Shared.DTOs;

namespace TIKR.Web.Services;

public class TikrApiClient(HttpClient http)
{
    public async Task<List<DashboardPriority>> GetDashboardPrioritiesAsync() =>
        await http.GetFromJsonAsync<List<DashboardPriority>>("/api/ai/dashboard-priorities") ?? [];

    public async Task<List<RequirementDto>> GetRequirementsAsync() =>
        await http.GetFromJsonAsync<List<RequirementDto>>("/api/requirements") ?? [];

    public async Task<List<DocumentDto>> GetDocumentsAsync(string? query = null)
    {
        var url = string.IsNullOrWhiteSpace(query) ? "/api/documents" : $"/api/documents?q={Uri.EscapeDataString(query)}";
        return await http.GetFromJsonAsync<List<DocumentDto>>(url) ?? [];
    }

    public async Task<List<KnowledgeEntryDto>> GetKnowledgeEntriesAsync() =>
        await http.GetFromJsonAsync<List<KnowledgeEntryDto>>("/api/knowledge") ?? [];

    public async Task<AiStatusResponse?> GetAiStatusAsync() =>
        await http.GetFromJsonAsync<AiStatusResponse>("/api/ai/status");

    public async Task<HttpResponseMessage> UploadDocumentAsync(Stream content, string fileName) =>
        await http.PostAsync("/api/documents", new MultipartFormDataContent
        {
            { new StreamContent(content), "file", fileName }
        });

    public async Task<TagDocumentResponse?> TagDocumentAsync(Guid documentId)
    {
        var response = await http.PostAsJsonAsync("/api/ai/tag-document", new TagDocumentRequest(documentId));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TagDocumentResponse>()
            : null;
    }

    public async Task<AskAdvancedResponse?> AskAdvancedAsync(string prompt, string? context = null)
    {
        var response = await http.PostAsJsonAsync("/api/ai/ask-advanced", new AskAdvancedRequest(prompt, context));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<AskAdvancedResponse>()
            : null;
    }

    public async Task CreateRequirementAsync(CreateRequirementRequest request) =>
        await http.PostAsJsonAsync("/api/requirements", request);

    public async Task CreateKnowledgeEntryAsync(CreateKnowledgeEntryRequest request) =>
        await http.PostAsJsonAsync("/api/knowledge", request);

    public async Task<SemanticSearchResponse?> SemanticSearchDocumentsAsync(string query, int topK = 3)
    {
        var response = await http.PostAsJsonAsync("/api/ai/semantic-search", new SemanticSearchRequest(query, topK));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SemanticSearchResponse>()
            : null;
    }

    public async Task<EmbedDocumentResponse?> EmbedDocumentAsync(Guid documentId)
    {
        var response = await http.PostAsync($"/api/ai/embed-document/{documentId}", content: null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<EmbedDocumentResponse>()
            : null;
    }

    public async Task<SemanticSearchKnowledgeResponse?> SemanticSearchKnowledgeAsync(string query, int topK = 3)
    {
        var response = await http.PostAsJsonAsync("/api/ai/semantic-search-knowledge", new SemanticSearchRequest(query, topK));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<SemanticSearchKnowledgeResponse>()
            : null;
    }

    public async Task<EmbedKnowledgeEntryResponse?> EmbedKnowledgeEntryAsync(Guid entryId)
    {
        var response = await http.PostAsync($"/api/ai/embed-knowledge/{entryId}", content: null);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<EmbedKnowledgeEntryResponse>()
            : null;
    }
}
