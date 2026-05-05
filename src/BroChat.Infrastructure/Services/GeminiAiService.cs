using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BroChat.Application.Interfaces;
using BroChat.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace BroChat.Infrastructure.Services;

public class GeminiAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiAiService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new ArgumentNullException("Gemini API Key is missing");
        _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        IEnumerable<Message> history, 
        string newPrompt, 
        IEnumerable<FileAttachment>? attachments = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var requestUrl = $"v1beta/models/gemini-3-flash-preview:streamGenerateContent?alt=sse&key={_apiKey}";

        // Ensure alternating roles for Gemini
        var contents = new List<object>();
        string lastRole = "";
 
        foreach (var m in history.OrderBy(m => m.Timestamp))
        {
            var currentRole = m.Role == Domain.Enums.MessageRole.User ? "user" : "model";
            if (currentRole == lastRole) continue;

            var parts = new List<object>();
            parts.Add(new { text = m.Content });

            if (m.Attachments != null && m.Attachments.Any())
            {
                foreach (var attachment in m.Attachments)
                {
                    var filePart = await GetFilePartAsync(attachment.Url, attachment.Type);
                    if (filePart != null) parts.Add(filePart);
                }
            }

            contents.Add(new
            {
                role = currentRole,
                parts = parts.ToArray()
            });
            lastRole = currentRole;
        }

        // Add the new prompt
        var finalParts = new List<object> { new { text = newPrompt } };
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                var filePart = await GetFilePartAsync(attachment.Url, attachment.Type);
                if (filePart != null) finalParts.Add(filePart);
            }
        }
        
        // Check if the LAST message in history was from user and already had a file, 
        // we might want to handle that, but typically the "newPrompt" is the one associated with the NEW file.
        // For simplicity, we assume the file is already in history if we just saved it.
        
        if (lastRole != "user")
        {
            contents.Add(new
            {
                role = "user",
                parts = finalParts.ToArray()
            });
        }
        else
        {
            // Append to last user message if exists
            var lastContent = (dynamic)contents[^1];
            var existingParts = new List<object>(lastContent.parts);
            existingParts.Add(new { text = "\n" + newPrompt });
            
            contents[^1] = new
            {
                role = "user",
                parts = existingParts.ToArray()
            };
        }

        var requestBody = new { contents };
        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Gemini API Error ({response.StatusCode}): {errorBody}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("data: "))
            {
                var json = line.Substring(6);
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        if (!string.IsNullOrEmpty(text))
                        {
                            yield return text;
                        }
                    }
                    else if (firstCandidate.TryGetProperty("finishReason", out var reason) && reason.GetString() == "SAFETY")
                    {
                        yield return "[Response blocked by AI safety filters]";
                    }
                }
            }
        }
    }

    private async Task<object?> GetFilePartAsync(string url, string? fileType)
    {
        try
        {
            var bytes = await _httpClient.GetByteArrayAsync(url);
            var base64 = Convert.ToBase64String(bytes);
            var mimeType = fileType ?? "image/jpeg";

            return new
            {
                inlineData = new
                {
                    mimeType = mimeType,
                    data = base64
                }
            };
        }
        catch
        {
            return null;
        }
    }
}
