using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services;

public class S3StorageService : IStorageService
{
    private readonly string _bucketName;
    private readonly string _serviceUrl;
    private readonly AmazonS3Client _s3Client;

    public S3StorageService(IConfiguration configuration)
    {
        var accessKey = Environment.GetEnvironmentVariable("S3_ACCESS_KEY") 
                        ?? configuration["S3_ACCESS_KEY"] 
                        ?? throw new ArgumentNullException(nameof(configuration), "S3_ACCESS_KEY is not configured.");
                        
        var secretKey = Environment.GetEnvironmentVariable("S3_SECRET_KEY") 
                        ?? configuration["S3_SECRET_KEY"] 
                        ?? throw new ArgumentNullException(nameof(configuration), "S3_SECRET_KEY is not configured.");
                        
        _serviceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL") 
                      ?? configuration["S3_SERVICE_URL"] 
                      ?? throw new ArgumentNullException(nameof(configuration), "S3_SERVICE_URL is not configured.");
                      
        _bucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") 
                      ?? configuration["S3_BUCKET_NAME"] 
                      ?? throw new ArgumentNullException(nameof(configuration), "S3_BUCKET_NAME is not configured.");

        var config = new AmazonS3Config
        {
            ServiceURL = _serviceUrl,
            ForcePathStyle = true // S3 compatible storages like Viettel IDC require path style URL
        };

        _s3Client = new AmazonS3Client(accessKey, secretKey, config);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var key = $"uploads/{uniqueFileName}";

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead // Allow public read access for client apps
        };

        await _s3Client.PutObjectAsync(request);

        // Viettel Cloud Storage URLs are formed as: {ServiceURL}/{BucketName}/{Key}
        var serviceUrlClean = _serviceUrl.TrimEnd('/');
        return $"{serviceUrlClean}/{_bucketName}/{key}";
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            // Extract the key from URL
            // The URL format is: {ServiceURL}/{BucketName}/{Key}
            // For example: https://s3-hcm-r1.idc.viettel.com.vn/mybucket/uploads/filename.png
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath.TrimStart('/'); // returns: mybucket/uploads/filename.png
            
            // Check if path starts with bucket name
            if (path.StartsWith(_bucketName, StringComparison.OrdinalIgnoreCase))
            {
                // Key is: uploads/filename.png
                var key = path.Substring(_bucketName.Length).TrimStart('/');
                
                var request = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key
                };

                await _s3Client.DeleteObjectAsync(request);
            }
        }
        catch (Exception)
        {
            // Fail silently as per LocalStorageService design
        }
    }
}
