using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VCloset.Application.Interfaces
{
    public class AppReviewDto
    {
        public string ReviewId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int StarRating { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTime? LastModified { get; set; }
        public string? Device { get; set; }
    }

    public interface IGooglePlayReviewService
    {
        /// <summary>
        /// Fetches the latest reviews for the specified Google Play App.
        /// </summary>
        /// <param name="packageName">The package name of the app (e.g., com.sentinels.vcloset)</param>
        /// <returns>A list of AppReviewDto</returns>
        Task<List<AppReviewDto>> FetchAppReviewsAsync(string packageName);
    }
}
