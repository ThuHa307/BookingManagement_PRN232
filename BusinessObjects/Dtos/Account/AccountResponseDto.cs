using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Account
{
    public class AccountResponseDto
    {
        public int AccountId { get; set; }
        public string? Username { get; set; }
        public string Email { get; set; } = null!;
        public string IsActive { get; set; } = null!;
        public string AuthProvider { get; set; } = null!;
        public string? AuthProviderId { get; set; }
        public bool? IsOnline { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Role { get; set; } = null!;
        public bool? TwoFactorEnabled { get; set; }
        public UserProfileResponseDto? UserProfile { get; set; }
    }
}
