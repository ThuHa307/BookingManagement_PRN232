using System;
using System.Collections.Generic;

namespace BusinessObjects.Domains;

public partial class Account
{

    public int AccountId { get; set; }

    public string? Username { get; set; }

    public string Email { get; set; } = null!;

    public string? Password { get; set; }

    public string IsActive { get; set; } = null!;

    public string AuthProvider { get; set; } = null!;

    public string? AuthProviderId { get; set; }

    public bool? IsOnline { get; set; }

    public DateTime? LastActiveAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string Role { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public bool? TwoFactorEnabled { get; set; }

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();

    public virtual ICollection<Conversation> ConversationReceivers { get; set; } = new List<Conversation>();

    public virtual ICollection<Conversation> ConversationSenders { get; set; } = new List<Conversation>();

    public virtual ICollection<FavoritePost> FavoritePosts { get; set; } = new List<FavoritePost>();

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<PostApproval> PostApprovals { get; set; } = new List<PostApproval>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

    public virtual ICollection<PromoUsage> PromoUsages { get; set; } = new List<PromoUsage>();

    public virtual ICollection<QuickReplyTemplate> QuickReplyTemplates { get; set; } = new List<QuickReplyTemplate>();

    public virtual UserProfile? UserProfile { get; set; }
}
