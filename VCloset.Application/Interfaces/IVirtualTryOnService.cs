using System.Threading.Tasks;

namespace VCloset.Application.Interfaces;

public interface IVirtualTryOnService
{
    /// <summary>
    /// Gửi yêu cầu thử đồ ảo tới Fashn AI API.
    /// </summary>
    /// <param name="modelImageUrl">URL ảnh người mẫu (hoặc người dùng)</param>
    /// <param name="productImageUrl">URL ảnh quần áo</param>
    /// <param name="category">Phân loại: auto, tops, bottoms, one-pieces</param>
    /// <param name="restoreBackground">Giữ lại hậu cảnh của hình ảnh người mẫu</param>
    /// <param name="generationMode">Chế độ tạo ảnh: fast, balanced, quality</param>
    /// <returns>Prediction ID để kiểm tra trạng thái</returns>
    Task<string> RunTryOnAsync(string modelImageUrl, string productImageUrl, string category = "auto", bool restoreBackground = true, string generationMode = "fast");

    /// <summary>
    /// Kiểm tra trạng thái xử lý thử đồ ảo.
    /// </summary>
    /// <param name="predictionId">ID nhận từ RunTryOnAsync</param>
    /// <returns>Tuple chứa: (Status, OutputImageUrl, ErrorMessage)</returns>
    Task<(string Status, string? OutputUrl, string? Error)> GetTryOnStatusAsync(string predictionId);
}
