using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LocalStorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
    {
        _env = env;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "uploads")
    {
        // Define path: wwwroot/folder
        var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var uploadsFolder = Path.Combine(webRoot, folder);
        
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        // Generate unique filename
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        // Build URL
        var request = _httpContextAccessor.HttpContext?.Request;
        var baseUrl = request != null ? $"{request.Scheme}://{request.Host}" : "";
        var folderClean = folder.TrimEnd('/');
        
        return $"{baseUrl}/{folderClean}/{uniqueFileName}";
    }

    public Task DeleteFileAsync(string fileUrl)
    {
        try 
        {
            if (string.IsNullOrEmpty(fileUrl)) return Task.CompletedTask;
            
            var uri = new Uri(fileUrl);
            var path = uri.LocalPath.TrimStart('/'); // e.g. uploads/filename.png
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRoot, path.Replace('/', Path.DirectorySeparatorChar));
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch { /* Ignore or log error */ }
        
        return Task.CompletedTask;
    }
}
