using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using VCloset.Application.Interfaces;
using System;

namespace VCloset.Infrastructure.Services;

public class PhotoroomService : IBackgroundRemovalService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public PhotoroomService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["PHOTOROOM_API_KEY"] ?? throw new ArgumentNullException("PHOTOROOM_API_KEY is missing in .env file");
        
        _httpClient.BaseAddress = new Uri("https://sdk.photoroom.com/v1/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
    }

    public async Task<byte[]> RemoveBackgroundAsync(byte[] imageBytes, string fileName = "image.png")
    {
        using var content = new MultipartFormDataContent();
        
        var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(imageContent, "image_file", fileName);
        
        // Call Photoroom API
        var response = await _httpClient.PostAsync("segment", content);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Photoroom API Error: {response.StatusCode} - {error}");
        }

        return await response.Content.ReadAsByteArrayAsync();
    }
}
