using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Account
{
    public class AccountFilterDto
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Role { get; set; }
        public string? IsActive { get; set; }
        public string? AuthProvider { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SortBy { get; set; } = "CreatedAt";
        public string? SortOrder { get; set; } = "desc"; // asc or desc
    }
}
