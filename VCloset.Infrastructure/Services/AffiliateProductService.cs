using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VCloset.Application.DTOs.Affiliate.Requests;
using VCloset.Application.DTOs.Affiliate.Responses;
using VCloset.Application.Interfaces;
using VCloset.Domain.Entities;
using VCloset.Domain.Enums;

namespace VCloset.Infrastructure.Services
{
    public class AffiliateProductService : IAffiliateProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStorageService _storageService;
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AffiliateProductService(
            IUnitOfWork unitOfWork,
            IStorageService storageService,
            HttpClient httpClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _storageService = storageService;
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        // 1. Thêm sản phẩm tiếp thị liên kết Shopee mới
        public async Task<AffiliateProductResponseDto> CreateProductAsync(CreateAffiliateProductDto dto)
        {
            var uploadedImageUrl = await UploadProductImageFromUrlAsync(dto.ImageUrl, dto.ShopeeProductId);

            var product = new AffiliateProduct
            {
                Id = Guid.NewGuid(),
                ShopeeProductId = dto.ShopeeProductId,
                ShopeeShopId = dto.ShopeeShopId,
                Name = dto.Name,
                Description = dto.Description,
                ImageUrl = uploadedImageUrl,
                Price = dto.Price,
                OriginalPrice = dto.OriginalPrice,
                Category = dto.Category,
                AffiliateLink = dto.AffiliateLink,
                TrackingCode = string.IsNullOrWhiteSpace(dto.TrackingCode)
                    ? $"TRK_{dto.ShopeeProductId}_{Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper()}"
                    : dto.TrackingCode,
                IsTrending = dto.IsTrending,
                IsActive = dto.IsActive,
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AffiliateProducts.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            return new AffiliateProductResponseDto
            {
                Id = product.Id,
                ShopeeProductId = product.ShopeeProductId,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                AffiliateLink = product.AffiliateLink,
                Category = product.Category,
                IsTrending = product.IsTrending,
                IsActive = product.IsActive
            };
        }

        // 2. Ghi nhận lượt click của người dùng
        public async Task<AffiliateClickResponseDto> RecordClickAsync(int userId, RecordAffiliateClickDto dto)
        {
            var product = await _unitOfWork.AffiliateProducts.FindAsync(p => p.Id == dto.ProductId && p.IsActive);
            if (product == null)
                throw new Exception("Sản phẩm tiếp thị liên kết không tồn tại hoặc đã bị ẩn.");

            int? outfitInternalId = null;
            if (dto.OutfitId.HasValue)
            {
                var outfit = await _unitOfWork.CanvasOutfits.FindAsync(o => o.Id == dto.OutfitId.Value);
                if (outfit != null)
                {
                    outfitInternalId = outfit.InternalId;
                }
            }

            var clickId = Guid.NewGuid();

            string finalLink = product.AffiliateLink;
            if (!string.IsNullOrEmpty(finalLink))
            {
                string separator = finalLink.Contains("?") ? "&" : "?";
                finalLink = $"{finalLink}{separator}sub_id={clickId}";
            }

            var clickRecord = new AffiliateClick
            {
                Id = clickId,
                UserInternalId = userId,
                AffiliateProductInternalId = product.InternalId,
                OutfitInternalId = outfitInternalId,
                ClickSource = dto.ClickSource ?? "direct",
                ClickedAt = DateTime.UtcNow
            };

            await _unitOfWork.AffiliateClicks.AddAsync(clickRecord);

            product.ClickCount += 1;
            _unitOfWork.AffiliateProducts.Update(product);

            await _unitOfWork.SaveChangesAsync();

            return new AffiliateClickResponseDto
            {
                ClickId = clickId,
                TargetAffiliateLink = finalLink
            };
        }

        // 3. Đối soát đơn hàng tự động từ file CSV
        public async Task<int> ImportConversionsAsync(Stream csvStream)
        {
            using var reader = new StreamReader(csvStream);
            
            // Đọc dòng tiêu đề (Header)
            var header = await reader.ReadLineAsync();
            if (header == null) return 0;

            int count = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Tách cột bằng dấu phẩy
                var parts = line.Split(',');
                if (parts.Length < 6) continue;

                // Giả định định dạng file CSV:
                // Cột 0: shopee_order_id (Mã đơn hàng)
                // Cột 1: sub_id (ClickId của chúng ta dưới dạng UUID)
                // Cột 2: purchase_amount (Tổng số tiền mua)
                // Cột 3: commission_earned (Số tiền hoa hồng kiếm được)
                // Cột 4: status (Trạng thái đơn hàng: completed, pending, cancelled)
                // Cột 5: ordered_at (Thời gian đặt hàng)

                string orderId = parts[0].Trim();
                string subIdStr = parts[1].Trim();
                
                if (!Guid.TryParse(subIdStr, out Guid clickId)) continue;
                
                if (!decimal.TryParse(parts[2].Trim(), out decimal purchaseAmount)) purchaseAmount = 0;
                if (!decimal.TryParse(parts[3].Trim(), out decimal commissionEarned)) commissionEarned = 0;
                
                string statusStr = parts[4].Trim().ToLower();
                if (!DateTime.TryParse(parts[5].Trim(), out DateTime orderedAt)) orderedAt = DateTime.UtcNow;

                // Tìm click_id tương ứng trong DB để lấy thông tin User và Product
                var click = await _unitOfWork.AffiliateClicks.FindAsync(c => c.Id == clickId);
                if (click == null) continue; // Bỏ qua nếu click_id không có trong hệ thống

                var existingConversion = await _unitOfWork.AffiliateConversions.FindAsync(c => c.ShopeeOrderId == orderId);
                
                var mappedStatus = MapStatus(statusStr);

                if (existingConversion != null)
                {
                    // Cập nhật trạng thái và số tiền nếu đơn hàng đã tồn tại
                    existingConversion.Status = mappedStatus;
                    existingConversion.OrderAmount = purchaseAmount;
                    existingConversion.CommissionAmount = commissionEarned;
                    
                    if (mappedStatus == VCloset.Domain.Enums.CommissionStatus.Confirmed)
                        existingConversion.ConfirmedAt = DateTime.UtcNow;
                    else if (mappedStatus == VCloset.Domain.Enums.CommissionStatus.Paid)
                        existingConversion.PaidAt = DateTime.UtcNow;

                    _unitOfWork.AffiliateConversions.Update(existingConversion);
                }
                else
                {
                    // Tính tỷ lệ phần trăm hoa hồng (commission_rate được định nghĩa là precision 4, 3, tức là tỉ lệ dạng thập phân, e.g. 0.05 đại diện cho 5%)
                    decimal commissionRate = purchaseAmount > 0 ? (commissionEarned / purchaseAmount) : 0;

                    var conversion = new AffiliateConversion
                    {
                        Id = Guid.NewGuid(),
                        ClickId = clickId,
                        UserInternalId = click.UserInternalId,
                        AffiliateProductInternalId = click.AffiliateProductInternalId,
                        ShopeeOrderId = orderId,
                        OrderAmount = purchaseAmount,
                        CommissionRate = commissionRate,
                        CommissionAmount = commissionEarned,
                        Status = mappedStatus,
                        ConvertedAt = orderedAt
                    };

                    if (mappedStatus == VCloset.Domain.Enums.CommissionStatus.Confirmed)
                        conversion.ConfirmedAt = DateTime.UtcNow;
                    else if (mappedStatus == VCloset.Domain.Enums.CommissionStatus.Paid)
                    {
                        conversion.ConfirmedAt = DateTime.UtcNow;
                        conversion.PaidAt = DateTime.UtcNow;
                    }

                    await _unitOfWork.AffiliateConversions.AddAsync(conversion);

                    // Tăng số lượng conversion của sản phẩm
                    var product = await _unitOfWork.AffiliateProducts.FindAsync(p => p.InternalId == click.AffiliateProductInternalId);
                    if (product != null)
                    {
                        product.ConversionCount += 1;
                        _unitOfWork.AffiliateProducts.Update(product);
                    }
                }
                count++;
            }

            await _unitOfWork.SaveChangesAsync();
            return count;
        }

        // 4. Lấy danh sách sản phẩm dành cho Admin (kèm phân trang, lọc, tìm kiếm)
        public async Task<PagedAffiliateProductsResponse> GetAdminProductsAsync(int page, int pageSize, ClothingCategory? category, bool? isActive, string? search)
        {
            var query = _unitOfWork.AffiliateProducts.Query();

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                query = query.Where(p => p.Name.ToLowerInvariant().Contains(searchLower) || p.ShopeeProductId.Contains(searchLower));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(product => new AffiliateProductResponseDto
                {
                    Id = product.Id,
                    ShopeeProductId = product.ShopeeProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    AffiliateLink = product.AffiliateLink,
                    Category = product.Category,
                    IsTrending = product.IsTrending,
                    IsActive = product.IsActive
                })
                .ToListAsync();

            return new PagedAffiliateProductsResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // 5. Lấy danh sách sản phẩm hoạt động dành cho người dùng phối đồ
        public async Task<PagedAffiliateProductsResponse> GetClientProductsAsync(int page, int pageSize, ClothingCategory? category, string? search)
        {
            // Chỉ lấy sản phẩm đang hoạt động (IsActive = true)
            var query = _unitOfWork.AffiliateProducts.Query().Where(p => p.IsActive);

            if (category.HasValue)
            {
                query = query.Where(p => p.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                query = query.Where(p => p.Name.ToLowerInvariant().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.IsTrending) // Đẩy sản phẩm trending lên trước
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(product => new AffiliateProductResponseDto
                {
                    Id = product.Id,
                    ShopeeProductId = product.ShopeeProductId,
                    Name = product.Name,
                    ImageUrl = product.ImageUrl,
                    Price = product.Price,
                    AffiliateLink = product.AffiliateLink,
                    Category = product.Category,
                    IsTrending = product.IsTrending,
                    IsActive = product.IsActive
                })
                .ToListAsync();

            return new PagedAffiliateProductsResponse
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // 6. Lấy chi tiết sản phẩm theo ID
        public async Task<AffiliateProductResponseDto?> GetProductByIdAsync(Guid id)
        {
            var product = await _unitOfWork.AffiliateProducts.FindAsync(p => p.Id == id);
            if (product == null) return null;

            return new AffiliateProductResponseDto
            {
                Id = product.Id,
                ShopeeProductId = product.ShopeeProductId,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                AffiliateLink = product.AffiliateLink,
                Category = product.Category,
                IsTrending = product.IsTrending,
                IsActive = product.IsActive
            };
        }

        // 7. Cập nhật sản phẩm
        public async Task<AffiliateProductResponseDto> UpdateProductAsync(Guid id, UpdateAffiliateProductDto dto)
        {
            var product = await _unitOfWork.AffiliateProducts.FindAsync(p => p.Id == id);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm tiếp thị liên kết yêu cầu.");

            var uploadedImageUrl = await UploadProductImageFromUrlAsync(dto.ImageUrl, product.ShopeeProductId);

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.ImageUrl = uploadedImageUrl;
            product.Price = dto.Price;
            product.OriginalPrice = dto.OriginalPrice;
            product.Category = dto.Category;
            product.AffiliateLink = dto.AffiliateLink;
            product.IsTrending = dto.IsTrending;
            product.IsActive = dto.IsActive;
            product.SyncedAt = DateTime.UtcNow;

            _unitOfWork.AffiliateProducts.Update(product);
            await _unitOfWork.SaveChangesAsync();

            return new AffiliateProductResponseDto
            {
                Id = product.Id,
                ShopeeProductId = product.ShopeeProductId,
                Name = product.Name,
                ImageUrl = product.ImageUrl,
                Price = product.Price,
                AffiliateLink = product.AffiliateLink,
                Category = product.Category,
                IsTrending = product.IsTrending,
                IsActive = product.IsActive
            };
        }

        // 8. Xóa mềm sản phẩm (chuyển IsActive = false)
        public async Task DeleteProductAsync(Guid id)
        {
            var product = await _unitOfWork.AffiliateProducts.FindAsync(p => p.Id == id);
            if (product == null)
                throw new Exception("Không tìm thấy sản phẩm tiếp thị liên kết yêu cầu.");

            // Xóa mềm để tránh lỗi khóa ngoại trong các Outfit đã phối trước đó
            product.IsActive = false;
            product.SyncedAt = DateTime.UtcNow;

            _unitOfWork.AffiliateProducts.Update(product);
            await _unitOfWork.SaveChangesAsync();
        }

        private VCloset.Domain.Enums.CommissionStatus MapStatus(string statusStr)
        {
            return statusStr.ToLower() switch
            {
                "completed" or "confirmed" => VCloset.Domain.Enums.CommissionStatus.Confirmed,
                "paid" => VCloset.Domain.Enums.CommissionStatus.Paid,
                "cancelled" or "rejected" => VCloset.Domain.Enums.CommissionStatus.Rejected,
                _ => VCloset.Domain.Enums.CommissionStatus.Pending
            };
        }

        private async Task<string> UploadProductImageFromUrlAsync(string imageUrl, string shopeeProductId)
        {
            if (string.IsNullOrEmpty(imageUrl) || !imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return imageUrl;
            }

            // Tránh tải lại nếu ảnh đã được upload lên Cloud Storage (S3) của chúng ta
            var s3ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL");
            if (!string.IsNullOrEmpty(s3ServiceUrl) && imageUrl.Contains(s3ServiceUrl, StringComparison.OrdinalIgnoreCase))
            {
                return imageUrl;
            }

            // Tránh tải lại nếu ảnh đã được lưu ở Local Storage của chúng ta
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var localBaseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
                if (imageUrl.Contains(localBaseUrl, StringComparison.OrdinalIgnoreCase))
                {
                    return imageUrl;
                }
            }

            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    return imageUrl;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
                var extension = contentType switch
                {
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/webp" => ".webp",
                    _ => ".jpg"
                };

                var fileName = $"shopee_prod_{shopeeProductId}{extension}";
                using var stream = await response.Content.ReadAsStreamAsync();
                
                return await _storageService.UploadFileAsync(stream, fileName, contentType, "products");
            }
            catch
            {
                return imageUrl;
            }
        }
    }
}
