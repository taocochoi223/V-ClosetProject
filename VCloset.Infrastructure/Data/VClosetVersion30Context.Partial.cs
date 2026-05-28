using Microsoft.EntityFrameworkCore;
using VCloset.Domain.Entities;

namespace VCloset.Infrastructure.Data;

public partial class VClosetVersion30Context
{
    public virtual DbSet<UserDeviceToken> UserDeviceTokens { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserDeviceToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_device_tokens_pkey");
            entity.ToTable("user_device_tokens");

            entity.HasIndex(e => e.FcmToken, "user_device_tokens_fcm_token_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
            entity.Property(e => e.UserInternalId).HasColumnName("user_internal_id");
            entity.Property(e => e.FcmToken).HasColumnName("fcm_token");
            entity.Property(e => e.DeviceType).HasColumnName("device_type").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserInternalId)
                .HasConstraintName("user_device_tokens_user_internal_id_fkey")
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
