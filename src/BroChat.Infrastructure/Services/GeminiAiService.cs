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
        var requestUrl = $"v1beta/models/gemini-1.5-flash:streamGenerateContent?key={_apiKey}";

        var contents = history.Select(m => new
        {
            role = m.Role == Domain.Enums.MessageRole.User ? "user" : "model",
            parts = new[] { new { text = m.Content } }
        }).ToList();

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = newPrompt } }
        });

        var requestBody = new { contents };

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        
        // This is a simplified parsing for streamGenerateContent array response
        // Note: Google's streamGenerateContent returns a JSON array of response chunks.
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var chunk in document.RootElement.EnumerateArray())
            {
                if (chunk.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
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
                }
            }
        }
    }
}
