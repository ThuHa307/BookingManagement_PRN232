using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace BusinessObjects.Dtos.Account
{
    public class AccountCreateDto : IValidatableObject
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        public string? Username { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Role là bắt buộc")]
        public string Role { get; set; } = null!;

        public string IsActive { get; set; } = "A";
        public string AuthProvider { get; set; } = "local";
        public string? AuthProviderId { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // Validate email format
            if (!IsValidEmailFormat(Email))
            {
                results.Add(new ValidationResult("Email không đúng định dạng.", new[] { nameof(Email) }));
            }

            // Validate username if provided
            if (!string.IsNullOrEmpty(Username) && !IsValidUsernameFormat(Username))
            {
                results.Add(new ValidationResult("Username chỉ được chứa chữ cái, số, dấu gạch dưới và dấu gạch ngang, độ dài từ 3-50 ký tự.", new[] { nameof(Username) }));
            }

            // Validate password strength
            if (!IsValidPasswordFormat(Password))
            {
                results.Add(new ValidationResult("Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số.", new[] { nameof(Password) }));
            }

            // Validate role
            if (!IsValidRoleFormat(Role))
            {
                results.Add(new ValidationResult("Role phải là U (User), A (Admin), S (Staff) hoặc L (Landlord).", new[] { nameof(Role) }));
            }

            // Validate IsActive
            if (!IsValidIsActiveFormat(IsActive))
            {
                results.Add(new ValidationResult("IsActive phải là A (Active) hoặc D (Disabled).", new[] { nameof(IsActive) }));
            }

            return results;
        }

        // Private validation methods
        private static bool IsValidEmailFormat(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
                return emailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidUsernameFormat(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return true; // Username is optional

            // Username must be 3-50 characters, alphanumeric with underscore and dash allowed
            var usernameRegex = new Regex(@"^[a-zA-Z0-9_-]{3,50}$");
            return usernameRegex.IsMatch(username);
        }

        private static bool IsValidPasswordFormat(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // At least 8 characters, must contain at least one uppercase, one lowercase, one digit
            return password.Length >= 6 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }

        private static bool IsValidRoleFormat(string role)
        {
            return new[] { "U", "A", "S", "L" }.Contains(role);
        }

        private static bool IsValidIsActiveFormat(string isActive)
        {
            return new[] { "A", "D" }.Contains(isActive);
        }
    }
}
