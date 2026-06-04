using System.Net;
using System.Net.Http;
using System.Text;
using HROpsBot.Core.NLU;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HROpsBot.Core.Tests;

public class GeminiNluClientTests
{
    [Fact]
    public async Task ClassifyAsync_WhenApiReturnsJsonInFence_ParsesResult()
    {
        var apiPayload = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "```json\n{\"intent\":\"task.list\",\"confidence\":0.93,\"detectedLanguage\":\"ru\",\"entities\":{\"query\":\"мои задачи\"}}\n```"
                  }
                ]
              }
            }
          ]
        }
        """;

        var client = CreateClient(HttpStatusCode.OK, apiPayload);

        var result = await client.ClassifyAsync("покажи мои задачи");

        Assert.Equal(NluResult.Intents.TaskList, result.Intent);
        Assert.True(result.Confidence > 0.9);
        Assert.Equal("ru", result.DetectedLanguage);
        Assert.Equal("мои задачи", result.Entities["query"]);
    }

    [Fact]
    public async Task ClassifyAsync_WhenApiReturnsNonJsonText_ReturnsFallback()
    {
        var apiPayload = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "Привет! Я думаю, это запрос про отпуск"
                  }
                ]
              }
            }
          ]
        }
        """;

        var client = CreateClient(HttpStatusCode.OK, apiPayload);

        var result = await client.ClassifyAsync("отпуск");

        Assert.Equal(NluResult.Intents.Fallback, result.Intent);
        Assert.True(result.IsFallback);
    }

    [Fact]
    public async Task ClassifyAsync_WhenApiReturnsHttp500_ReturnsFallback()
    {
        var client = CreateClient(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}");

        var result = await client.ClassifyAsync("любой текст");

        Assert.Equal(NluResult.Intents.Fallback, result.Intent);
    }

    private static GeminiNluClient CreateClient(HttpStatusCode statusCode, string payload)
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });

        var http = new HttpClient(handler);
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Model = "gemini-test",
            BaseUrl = "https://example.test"
        });

        return new GeminiNluClient(http, options, NullLogger<GeminiNluClient>.Instance);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
