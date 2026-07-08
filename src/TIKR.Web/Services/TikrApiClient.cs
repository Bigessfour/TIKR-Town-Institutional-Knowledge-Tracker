using System.Net;
using System.Text.Json;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Web.Services;

public class TikrApiClient(HttpClient http)
{
    public async Task<List<DashboardPriority>> GetDashboardPrioritiesAsync() =>
        await http.GetFromJsonAsync<List<DashboardPriority>>("/api/ai/dashboard-priorities") ?? [];

    public async Task<List<RequirementDto>> GetRequirementsAsync() =>
        await http.GetFromJsonAsync<List<RequirementDto>>("/api/requirements") ?? [];

    public async Task<RequirementDto?> GetRequirementAsync(Guid id) =>
        await http.GetFromJsonAsync<RequirementDto>($"/api/requirements/{id}");

    public async Task UpdateRequirementAsync(Guid id, UpdateRequirementRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/requirements/{id}", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<DocumentDto>> GetDocumentsAsync(string? query = null)
    {
        var url = string.IsNullOrWhiteSpace(query) ? "/api/documents" : $"/api/documents?q={Uri.EscapeDataString(query)}";
        return await http.GetFromJsonAsync<List<DocumentDto>>(url) ?? [];
    }

    public async Task<List<KnowledgeEntryDto>> GetKnowledgeEntriesAsync() =>
        await http.GetFromJsonAsync<List<KnowledgeEntryDto>>("/api/knowledge") ?? [];

    public async Task<AiStatusResponse?> GetAiStatusAsync() =>
        await http.GetFromJsonAsync<AiStatusResponse>("/api/ai/status");

    public async Task<LocalStorageStatusDto?> GetLocalStorageStatusAsync() =>
        await http.GetFromJsonAsync<LocalStorageStatusDto>("/api/system/local-status");

    public async Task<DocumentSdkStatusDto?> GetDocumentSdkStatusAsync() =>
        await http.GetFromJsonAsync<DocumentSdkStatusDto>("/api/system/document-sdk-status");

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

    public async Task DeleteRequirementAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/requirements/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<KnowledgeEntryDto?> CreateKnowledgeEntryAsync(CreateKnowledgeEntryRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/knowledge", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<KnowledgeEntryDto>()
            : null;
    }

    public async Task UpdateKnowledgeEntryAsync(Guid id, UpdateKnowledgeEntryRequest request) =>
        await http.PutAsJsonAsync($"/api/knowledge/{id}", request);

    public async Task DeleteKnowledgeEntryAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/knowledge/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AuditLog>> GetRecentAuditAsync(int limit = 10) =>
        await http.GetFromJsonAsync<List<AuditLog>>($"/api/audit?limit={limit}") ?? [];

    public async Task DeleteDocumentAsync(Guid id)
    {
        var response = await http.DeleteAsync($"/api/documents/{id}");
        response.EnsureSuccessStatusCode();
    }

    public string GetDocumentContentUrl(Guid id) => $"/api/documents/{id}/content";

    public Uri GetAbsoluteDocumentContentUri(Guid id)
    {
        var baseUri = http.BaseAddress ?? new Uri("http://localhost:5000/");
        return new Uri(baseUri, GetDocumentContentUrl(id));
    }

    public async Task<byte[]?> GetDocumentContentAsync(Guid id)
    {
        var response = await http.GetAsync(GetDocumentContentUrl(id));
        return response.IsSuccessStatusCode
            ? await response.Content.ReadAsByteArrayAsync()
            : null;
    }

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

    public async Task<UserProfileDto?> GetProfileAsync() =>
        await http.GetFromJsonAsync<UserProfileDto>("/api/auth/me");

    public async Task<List<UserSummaryDto>> GetUsersAsync() =>
        await http.GetFromJsonAsync<List<UserSummaryDto>>("/api/auth/users") ?? [];

    public async Task<UserSummaryDto?> CreateUserAsync(CreateUserRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/auth/users", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserSummaryDto>()
            : null;
    }

    public async Task<UserSummaryDto?> UpdateUserAsync(string id, UpdateUserRequest request)
    {
        var response = await http.PutAsJsonAsync($"/api/auth/users/{id}", request);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<UserSummaryDto>()
            : null;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var response = await http.PostAsJsonAsync("/api/auth/change-password", request);
        return response.IsSuccessStatusCode;
    }

    public async Task<DocumentAgentResult?> ScanDocumentWithAgentAsync(Stream content, string fileName)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StreamContent(content), "file", fileName);
        var response = await http.PostAsync("/api/ai/agent-scan", multipart);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<DocumentAgentResult>()
            : null;
    }

    public Task<DocumentGenerationResponse> GenerateCouncilAgendaPdfAsync(CouncilAgendaRequest? request = null) =>
        PostGeneratedDocumentAsync("/api/documents/generate/council-agenda", request);

    public Task<DocumentGenerationResponse> GenerateComplianceReportXlsxAsync(ComplianceReportRequest? request = null) =>
        PostGeneratedDocumentAsync("/api/documents/generate/compliance-report", request);

    public Task<DocumentGenerationResponse> GenerateMeetingMinutesDocxAsync(MeetingMinutesRequest request) =>
        PostGeneratedDocumentAsync("/api/documents/generate/meeting-minutes", request);

    public Task<DocumentGenerationResponse> GenerateClerkMemoDocxAsync(ClerkMemoRequest request) =>
        PostGeneratedDocumentAsync("/api/documents/generate/clerk-memo", request);

    public async Task<DocumentGenerationResponse> ConvertWordToPdfAsync(Stream wordContent, string fileName)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StreamContent(wordContent), "file", fileName);
        return await ParseGenerationResponseAsync(await http.PostAsync("/api/documents/convert/word-to-pdf", multipart));
    }

    public async Task<DocumentGenerationResponse> ConvertExcelToPdfAsync(Stream excelContent, string fileName)
    {
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new StreamContent(excelContent), "file", fileName);
        return await ParseGenerationResponseAsync(await http.PostAsync("/api/documents/convert/excel-to-pdf", multipart));
    }

    public async Task<DocumentGenerationResponse> ConvertStoredDocumentToPdfAsync(Guid documentId, string fileName)
    {
        var bytes = await GetDocumentContentAsync(documentId);
        if (bytes is null || bytes.Length == 0)
            return new DocumentGenerationResponse(null, $"Could not read {fileName} from NAS storage.");

        await using var stream = new MemoryStream(bytes);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".xls" or ".xlsx"
            ? await ConvertExcelToPdfAsync(stream, fileName)
            : await ConvertWordToPdfAsync(stream, fileName);
    }

    private Task<DocumentGenerationResponse> PostGeneratedDocumentAsync<T>(string path, T? body) =>
        ParseGenerationResponseAsync(http.PostAsJsonAsync(path, body));

    private static async Task<DocumentGenerationResponse> ParseGenerationResponseAsync(Task<HttpResponseMessage> responseTask)
    {
        var response = await responseTask;
        return await ParseGenerationResponseAsync(response);
    }

    private static async Task<DocumentGenerationResponse> ParseGenerationResponseAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var error = await TryReadGenerationErrorAsync(response);
            return new DocumentGenerationResponse(null, error);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var disposition = response.Content.Headers.ContentDisposition;
        var fileName = disposition?.FileNameStar
            ?? disposition?.FileName?.Trim('"')
            ?? "generated-document.bin";
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
        return new DocumentGenerationResponse(new GeneratedDocumentDownloadDto(bytes, fileName, contentType), null);
    }

    private static async Task<string> TryReadGenerationErrorAsync(HttpResponseMessage response)
    {
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
        {
            try
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (body.TryGetProperty("error", out var error) && error.GetString() is { } message)
                    return message;
            }
            catch
            {
                // Fall through to default message.
            }
        }

        return response.StatusCode == HttpStatusCode.ServiceUnavailable
            ? "Syncfusion Document SDK is not licensed. Set SYNCFUSION_LICENSE_KEY on the API container."
            : $"Document generation failed ({(int)response.StatusCode}).";
    }
}
