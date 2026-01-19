using HrLite.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Text.Json;

namespace HrLite.Infrastructure.AI;

public class OpenAiLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly bool _enableAiFeatures;
    private readonly int _timeoutSeconds;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public OpenAiLlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        
        _enableAiFeatures = configuration.GetValue<bool>("Ai:EnableAiFeatures", false);
        _timeoutSeconds = configuration.GetValue<int>("Ai:TimeoutSeconds", 15);
        _maxTokens = configuration.GetValue<int>("Ai:MaxTokens", 800);
        _temperature = configuration.GetValue<double>("Ai:Temperature", 0.2);
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_timeoutSeconds);
    }

    public async Task<string> GenerateInsightsAsync(string aggregatedData, CancellationToken cancellationToken = default)
    {
        if (!_enableAiFeatures)
        {
            return JsonSerializer.Serialize(new
            {
                summary = "AI features are currently disabled. Enable them in configuration to get AI-powered insights.",
                insights = new[] { "No AI insights available" },
                recommendedActions = new[] { "Enable AI features in appsettings.json" }
            });
        }

        var apiKey = _configuration["Ai:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return JsonSerializer.Serialize(new
            {
                summary = "AI API key not configured.",
                insights = new[] { "Configure Ai:ApiKey in appsettings.json" },
                recommendedActions = new[] { "Add your OpenAI API key to configuration" }
            });
        }

        var prompt = $@"You are an HR analytics expert. Analyze the following aggregated HR data and provide insights.

Data:
{aggregatedData}

Respond ONLY with valid JSON in this exact format (no markdown, no code blocks):
{{
  ""summary"": ""A brief 2-3 sentence overview of the data"",
  ""insights"": [""insight 1"", ""insight 2"", ""insight 3""],
  ""recommendedActions"": [""action 1"", ""action 2"", ""action 3""]
}}

Rules:
- Do NOT include any employee names or personal data
- Focus on aggregated trends and patterns
- Provide actionable recommendations
- Keep insights concise and business-focused";

        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "system", content = "You are an HR analytics expert. Always respond with valid JSON only, no markdown formatting." },
                new { role = "user", content = prompt }
            },
            temperature = _temperature,
            max_tokens = _maxTokens
        };

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "https://api.openai.com/v1/chat/completions",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var content = result.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("Empty response from LLM");
            }

            // Clean up response (remove markdown code blocks if present)
            content = content.Trim();
            if (content.StartsWith("```json"))
            {
                content = content.Substring(7);
            }
            if (content.StartsWith("```"))
            {
                content = content.Substring(3);
            }
            if (content.EndsWith("```"))
            {
                content = content.Substring(0, content.Length - 3);
            }

            return content.Trim();
        }
        catch (HttpRequestException ex)
        {
            return JsonSerializer.Serialize(new
            {
                summary = $"Failed to connect to AI service: {ex.Message}",
                insights = new[] { "AI service is currently unavailable" },
                recommendedActions = new[] { "Check API key and network connectivity" }
            });
        }
        catch (TaskCanceledException)
        {
            throw;
        }
    }
}
