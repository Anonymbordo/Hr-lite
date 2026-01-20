using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HrLite.Application.DTOs;
using HrLite.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HrLite.Infrastructure.AI;

public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<JobDescriptionDraftDto> GenerateJobDescriptionAsync(string role, string department)
    {
        var apiKey = _configuration["Ai:ApiKey"];
        var baseUrl = _configuration["Ai:BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
        var model = _configuration["Ai:Model"] ?? "llama3-8b-8192";

        if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YourSuper"))
        {
            throw new InvalidOperationException("AI API key is missing. Configure Ai:ApiKey in appsettings.");
        }

        var systemPrompt = "Sen uzman bir İnsan Kaynakları (HR) danışmanısın. Verilen pozisyon ve departman için Türkçe bir görev tanımı taslağı üret. Cevabı SADECE JSON formatında döndür; markdown, serbest metin veya ek açıklama ekleme. Özel/Kişisel bilgi ekleme.";
        var userPrompt = $@"Pozisyon: {role}
Departman: {department}

SADECE şu JSON formatında cevap ver (markdown veya başka format kullanma):
{{
  ""titleSuggested"": ""önerilen pozisyon adı"",
  ""responsibilities"": [""sorumluluk 1"", ""sorumluluk 2"", ""sorumluluk 3""],
  ""requirements"": [""gereklilik 1"", ""gereklilik 2"", ""gereklilik 3""],
  ""niceToHave"": [""artı 1"", ""artı 2""],
  ""jobDescription"": ""tek paragraf iş tanımı""
}}";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 1000,
            response_format = new { type = "json_object" }
        };

        // 3. İsteği Gönder
        var request = new HttpRequestMessage(HttpMethod.Post, baseUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(requestBody);

        try
        {
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI error response {Status}: {Error}", response.StatusCode, error);
                throw new InvalidOperationException("AI service returned an error. See logs for details.");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<GroqResponse>();

            var content = jsonResponse?.Choices?.FirstOrDefault()?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("AI returned empty content.");
            }

            try
            {
                var draft = JsonSerializer.Deserialize<JobDescriptionDraftDto>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (draft == null)
                {
                    throw new InvalidOperationException("AI response could not be parsed.");
                }

                return draft;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response: {Content}", content);
                throw new InvalidOperationException("AI response was not valid JSON.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API çağrısı başarısız");
            throw;
        }
    }

    // Groq'tan gelen JSON cevabını karşılayacak sınıflar
    private class GroqResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}