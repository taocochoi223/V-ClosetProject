using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VCloset.Application.DTOs;
using VCloset.Domain.Enums;

namespace VCloset.Application.Interfaces;

public interface IWardrobeService
{
    Task<WardrobeItemResponseDto> CreateItemAsync(int userInternalId, CreateWardrobeItemDto dto);
    Task<WardrobeItemResponseDto> GetItemByIdAsync(int userInternalId, Guid itemId);
    Task<List<WardrobeItemResponseDto>> GetItemsAsync(int userInternalId, ClothingCategory? category = null, string? color = null);
    Task<WardrobeItemResponseDto> UpdateItemAsync(int userInternalId, Guid itemId, UpdateWardrobeItemDto dto);
    Task DeleteItemAsync(int userInternalId, Guid itemId);
}
