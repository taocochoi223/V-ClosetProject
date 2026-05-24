using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VCloset.Domain.Entities;

public class UserFollower
{
    [Key]
    public int Id { get; set; }
    public int FollowerId { get; set; }
    [ForeignKey("FollowerId")]
    public virtual User Follower { get; set; } = null!;
    public int FollowingId { get; set; }
    [ForeignKey("FollowingId")]
    public virtual User Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
