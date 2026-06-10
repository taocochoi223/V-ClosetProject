using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class FashnTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private readonly string _apiKey;

    public FashnTryOnService(HttpClient httpClient, IConfiguration configuration, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _apiKey = configuration["FASHN_API_KEY"] ?? throw new ArgumentNullException("FASHN_API_KEY is missing in .env file");

        _httpClient.BaseAddress = new Uri("https://api.fashn.ai/v1/");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> RunTryOnAsync(string modelImageUrl, string productImageUrl, string category = "auto", bool restoreBackground = true)
    {
        var mappedCategory = MapCategory(category);

        var payload = new FashnRunRequest
        {
            ModelName = "tryon-max",
            Inputs = new FashnInputs
            {
                ModelImage = modelImageUrl,
                ProductImage = productImageUrl
            }
        };

        var response = await _httpClient.PostAsJsonAsync("run", payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Fashn AI Run Error: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<FashnRunResponse>();
        if (result == null || string.IsNullOrEmpty(result.Id))
        {
            throw new Exception("Fashn AI returned an invalid or empty response.");
        }

        var errorMsg = GetErrorString(result.Error);
        if (!string.IsNullOrEmpty(errorMsg))
        {
            throw new Exception($"Fashn AI Prediction Error: {errorMsg}");
        }

        return result.Id;
    }

    public async Task<(string Status, string? OutputUrl, string? Error)> GetTryOnStatusAsync(string predictionId)
    {
        var response = await _httpClient.GetAsync($"status/{predictionId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            return ("failed", null, $"Fashn AI Status Error: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<FashnStatusResponse>();
        if (result == null)
        {
            return ("failed", null, "Failed to parse Fashn AI status response.");
        }

        var status = result.Status?.ToLowerInvariant() ?? "failed";

        if (status == "completed" && result.Output != null && result.Output.Length > 0)
        {
            var cdnUrl = result.Output[0];

            // Host permanently on S3
            try
            {
                using var client = new HttpClient();
                var imageBytes = await client.GetByteArrayAsync(cdnUrl);
                
                using var ms = new MemoryStream(imageBytes);
                var s3Url = await _storageService.UploadFileAsync(ms, $"tryon_{predictionId}.png", "image/png");
                
                return (status, s3Url, null);
            }
            catch (Exception ex)
            {
                // Fallback to Fashn CDN URL if S3 upload fails
                Console.WriteLine($"[WARNING] S3 Upload for TryOn result failed, falling back to CDN URL: {ex.Message}");
                return (status, cdnUrl, null);
            }
        }

        return (status, null, GetErrorString(result.Error));
    }

    private static string MapCategory(string category)
    {
        var normalized = category.Trim().ToLowerInvariant();
        if (normalized == "top" || normalized == "tops") return "tops";
        if (normalized == "bottom" || normalized == "bottoms") return "bottoms";
        if (normalized == "onepieces" || normalized == "onepiece" || normalized == "one-piece" || normalized == "one-pieces" || normalized == "dress") return "one-pieces";
        return "auto";
    }

    // --- Inner Models for Fashn AI API ---

    private class FashnRunRequest
    {
        [JsonPropertyName("model_name")]
        public string ModelName { get; set; } = "tryon-max";

        [JsonPropertyName("inputs")]
        public FashnInputs Inputs { get; set; } = null!;
    }

    private class FashnInputs
    {
        [JsonPropertyName("model_image")]
        public string ModelImage { get; set; } = null!;

        [JsonPropertyName("product_image")]
        public string ProductImage { get; set; } = null!;
    }

    private static string? GetErrorString(JsonElement? errorElement)
    {
        if (errorElement == null || errorElement.Value.ValueKind == JsonValueKind.Null)
            return null;

        if (errorElement.Value.ValueKind == JsonValueKind.String)
            return errorElement.Value.GetString();

        if (errorElement.Value.ValueKind == JsonValueKind.Object)
        {
            if (errorElement.Value.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
            {
                return msgProp.GetString();
            }
        }

        return errorElement.Value.ToString();
    }

    private class FashnRunResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("error")]
        public JsonElement? Error { get; set; }
    }

    private class FashnStatusResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = null!;

        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("output")]
        public string[]? Output { get; set; }

        [JsonPropertyName("error")]
        public JsonElement? Error { get; set; }
    }
}
