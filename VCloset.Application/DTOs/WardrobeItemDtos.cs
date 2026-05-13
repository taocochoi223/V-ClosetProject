using System;
using System.Collections.Generic;
using VCloset.Domain.Enums;

namespace VCloset.Application.DTOs;

public class CreateWardrobeItemDto
{
    public string? Name { get; set; }
    public string OriginalImageUrl { get; set; } = null!;
    public ClothingCategory Category { get; set; }
    public List<string>? ColorTags { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
}

public class UpdateWardrobeItemDto
{
    public string? Name { get; set; }
    public ClothingCategory? Category { get; set; }
    public List<string>? ColorTags { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
}

public class WardrobeItemResponseDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string OriginalImageUrl { get; set; } = null!;
    public string? RemovedBgUrl { get; set; }
    public AiJobStatus BgRemovalStatus { get; set; }
    public string Category { get; set; } = null!;
    public List<string> ColorTags { get; set; } = new List<string>();
    public string? Brand { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}
