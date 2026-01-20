using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public async Task<string> GenerateJobDescriptionAsync(string role, string department)
    {
        // 1. Ayarları Oku
        var apiKey = _configuration["Ai:ApiKey"];
        var baseUrl = _configuration["Ai:BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
        var model = _configuration["Ai:Model"] ?? "llama3-8b-8192";

        if (string.IsNullOrEmpty(apiKey) || apiKey.StartsWith("YourSuper"))
        {
            return "⚠️ API Anahtarı eksik! Lütfen appsettings.json dosyasina Groq API anahtarini giriniz.";
        }

        // 2. Yapay Zekaya Gönderilecek Mesajı Hazırla
        var systemPrompt = "Sen uzman bir İnsan Kaynakları (HR) danışmanısın. Verilen pozisyon ve departman için profesyonel, maddeler halinde ve Türkçe bir görev tanımı (Job Description) hazırla. Giriş cümlesi yazma, direkt içeriğe gir.";
        var userPrompt = $"Pozisyon: {role}\nDepartman: {department}\n\nLütfen bu pozisyon için sorumluluklar, aranan nitelikler ve genel beklentileri içeren bir taslak oluştur.";

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.7,
            max_tokens = 1000
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
                _logger.LogError($"AI Hatası: {error}");
                return $"AI Servis Hatası: {response.StatusCode}. Lütfen logları kontrol edin.";
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<GroqResponse>();
            
            // Cevabı al ve döndür
            return jsonResponse?.Choices?.FirstOrDefault()?.Message?.Content 
                   ?? "Yapay zeka boş bir cevap döndürdü.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Groq API bağlantı hatası");
            return $"Bağlantı Hatası: {ex.Message}";
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