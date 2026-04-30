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

            contents.Add(new
            {
                role = currentRole,
                parts = new[] { new { text = m.Content } }
            });
            lastRole = currentRole;
        }

        if (lastRole != "user")
        {
            contents.Add(new
            {
                role = "user",
                parts = new[] { new { text = newPrompt } }
            });
        }
        else
        {
            var lastContent = (dynamic)contents[^1];
            contents[^1] = new
            {
                role = "user",
                parts = new[] { new { text = lastContent.parts[0].text + "\n" + newPrompt } }
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
}
