using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VCloset.Application.DTOs;

public class CreateOutfitDto
{
    public string Title { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    // Danh sách các item trên canvas
    public List<CanvasItemDto> Items { get; set; } = new();
}

public class CanvasItemDto
{
    // Item này là đồ cá nhân hay đồ Shopee?
    public int? WardrobeItemInternalId { get; set; }
    public int? AffiliateProductInternalId { get; set; }

    // Các thông số 2D
    public decimal PosX { get; set; }
    public decimal PosY { get; set; }
    public decimal Scale { get; set; } = 1;
    public decimal Rotation { get; set; }
    public short ZIndex { get; set; }
}

public class OutfitResponseDto
{
    public Guid Id { get; set; } // UUID để trả về Frontend
    public string Title { get; set; } = string.Empty;
    public string? CanvasSnapshotUrl { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
}

