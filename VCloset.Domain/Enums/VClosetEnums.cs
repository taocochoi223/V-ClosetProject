namespace VCloset.Domain.Enums
{
    // Auth & Identity
    public enum UserRole
    {
        Customer,
        Admin,
        Moderator,
        BrandPartner
    }

    public enum AuthProvider
    {
        Local,
        Google
    }

    // Wardrobe & AI
    public enum BodyShapeType
    {
        Hourglass,
        Pear,
        Apple,
        Rectangle,
        InvertedTriangle
    }

    public enum ClothingCategory
    {
        Top,
        Bottom,
        Dress,
        Outerwear,
        Shoes,
        Bag,
        Accessory,
        Other
    }

    public enum AiJobStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }

    // Affiliate
    public enum CommissionStatus
    {
        Pending,
        Confirmed,
        Paid,
        Rejected
    }

    // Billing
    public enum PremiumPlan
    {
        Monthly,
        Yearly
    }

    // Brand
    public enum BrandStatus
    {
        Pending,
        Verified,
        Suspended
    }

    // Chat
    public enum ChatRoomType
    {
        Public,
        Topic,
        Direct
    }

    public enum MessageType
    {
        Text,
        Image,
        OutfitShare,
        System
    }

    // Ban
    public enum BanType
    {
        Chat,
        Post,
        Account
    }

    // Payment
    public enum PaymentStatus
    {
        Pending,
        Success,
        Failed,
        Cancelled,
        Expired
    }
}
