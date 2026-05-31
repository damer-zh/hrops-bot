using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HROpsBot.Core.NLU;

public class GeminiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}

public class GeminiNluClient(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiNluClient> logger)
{
    private readonly GeminiOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<NluResult> ClassifyAsync(string userMessage, CancellationToken ct = default)
    {
        var fallback = new NluResult { Intent = NluResult.Intents.Fallback };

        try
        {
            var url = $"{_options.BaseUrl}/models/{_options.Model}:generateContent?key={_options.ApiKey}";
            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = NluPromptBuilder.SystemPrompt } }
                },
                contents = new[]
                {
                    new { parts = new[] { new { text = userMessage } } }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 1024
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(url, content, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini API error: {StatusCode}", response.StatusCode);
                return fallback;
            }

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(JsonOpts, ct);
            var rawJson = geminiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text;

            if (string.IsNullOrWhiteSpace(rawJson))
                return fallback;

            rawJson = rawJson.Trim();
            if (rawJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                rawJson = rawJson.Substring(7).Trim();
            else if (rawJson.StartsWith("```"))
                rawJson = rawJson.Substring(3).Trim();
                
            if (rawJson.EndsWith("```"))
                rawJson = rawJson.Substring(0, rawJson.Length - 3).Trim();

            if (!rawJson.StartsWith("{"))
            {
                int startIndex = rawJson.IndexOf('{');
                int endIndex = rawJson.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    rawJson = rawJson.Substring(startIndex, endIndex - startIndex + 1);
                }
                else
                {
                    logger.LogWarning("Gemini API returned non-JSON text: {Text}", rawJson);
                    return fallback;
                }
            }

            try
            {
                var result = JsonSerializer.Deserialize<NluResult>(rawJson, JsonOpts);
                return result ?? fallback;
            }
            catch (JsonException jex)
            {
                logger.LogError(jex, "Failed to parse JSON from Gemini. Raw string was:\n{RawJson}", rawJson);
                return fallback;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to classify intent for message: {Message}", userMessage);
            return fallback;
        }
    }

    // Gemini API response DTOs
    private record GeminiResponse(
        [property: JsonPropertyName("candidates")] GeminiCandidate[]? Candidates);

    private record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);

    private record GeminiContent(
        [property: JsonPropertyName("parts")] GeminiPart[]? Parts);

    private record GeminiPart(
        [property: JsonPropertyName("text")] string? Text);
}
