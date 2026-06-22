using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using VCloset.Application.Interfaces;

namespace VCloset.Infrastructure.Services
{
    public class GooglePlayReviewService : IGooglePlayReviewService
    {
        private readonly IConfiguration _configuration;

        public GooglePlayReviewService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<AppReviewDto>> FetchAppReviewsAsync(string packageName)
        {
            var results = new List<AppReviewDto>();

            // Lấy JSON key từ biến môi trường (hoặc appsettings.json)
            var jsonKey = Environment.GetEnvironmentVariable("GOOGLE_PLAY_CREDENTIAL_JSON") 
                          ?? _configuration["GOOGLE_PLAY_CREDENTIAL_JSON"];

            if (string.IsNullOrEmpty(jsonKey))
            {
                Console.WriteLine("[ERROR] GOOGLE_PLAY_CREDENTIAL_JSON is not configured.");
                return results; // Return empty list if not configured
            }

            try
            {
                // Xác thực bằng chuỗi JSON
                GoogleCredential credential = GoogleCredential.FromJson(jsonKey)
                    .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);

                var service = new AndroidPublisherService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "V-Closet Application",
                });

                // Gọi API lấy danh sách đánh giá
                var request = service.Reviews.List(packageName);
                // Giới hạn số lượng trả về (ví dụ 100 comment mới nhất)
                request.MaxResults = 100;
                
                var response = await request.ExecuteAsync();

                if (response.Reviews != null)
                {
                    foreach (var review in response.Reviews)
                    {
                        var userComment = review.Comments?.FirstOrDefault()?.UserComment;
                        if (userComment != null)
                        {
                            // Parse LastModified from Google Timestamp
                            DateTime? lastModifiedDate = null;
                            if (userComment.LastModified?.Seconds != null)
                            {
                                lastModifiedDate = DateTimeOffset.FromUnixTimeSeconds(userComment.LastModified.Seconds.Value).UtcDateTime;
                            }

                            results.Add(new AppReviewDto
                            {
                                ReviewId = review.ReviewId,
                                AuthorName = review.AuthorName,
                                StarRating = userComment.StarRating ?? 0,
                                Text = userComment.Text,
                                LastModified = lastModifiedDate,
                                Device = userComment.DeviceMetadata?.ProductName
                            });
                        }
                    }
                }
                
                Console.WriteLine($"[INFO] Successfully fetched {results.Count} reviews from Google Play for {packageName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch Google Play Reviews: {ex.Message}");
            }

            return results;
        }
    }
}
