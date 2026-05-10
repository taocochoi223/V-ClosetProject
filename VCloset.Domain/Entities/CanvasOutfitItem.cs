using System;
using System.Collections.Generic;

namespace VCloset.Domain.Entities;

/// <summary>
/// V? trí t?ng item trên canvas. Ð? nhà HO?C affiliate — CHECK constraint d?m b?o ch? 1 trong 2.
/// </summary>
public partial class CanvasOutfitItem
{
    public int Id { get; set; }

    public int OutfitInternalId { get; set; }

    public int? WardrobeItemInternalId { get; set; }

    public int? AffiliateProductInternalId { get; set; }

    public decimal PosX { get; set; }

    public decimal PosY { get; set; }

    public decimal Scale { get; set; }

    public decimal Rotation { get; set; }

    public short ZIndex { get; set; }

    public virtual AffiliateProduct? AffiliateProductInternal { get; set; }

    public virtual CanvasOutfit OutfitInternal { get; set; } = null!;

    public virtual WardrobeItem? WardrobeItemInternal { get; set; }
}

