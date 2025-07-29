using System.Text.RegularExpressions;

namespace WebAPI.Extensions
{
    public static class ValidationExtensions
    {
        public static bool IsValidEmail(this string email)
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

        public static bool IsValidUsername(this string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return true; // Username is optional

            // Username must be 3-50 characters, alphanumeric with underscore and dash allowed
            var usernameRegex = new Regex(@"^[a-zA-Z0-9_-]{3,50}$");
            return usernameRegex.IsMatch(username);
        }

        public static bool IsValidPassword(this string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // At least 8 characters, must contain at least one uppercase, one lowercase, one digit
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }

        public static bool IsValidRole(this string role)
        {
            return new[] { "U", "A", "S", "L" }.Contains(role);
        }

        public static bool IsValidIsActive(this string isActive)
        {
            return new[] { "A", "D" }.Contains(isActive);
        }

        public static string GetRoleDisplayName(this string role)
        {
            return role switch
            {
                "U" => "User",
                "A" => "Admin",
                "S" => "Staff",
                "L" => "Landlord",
                _ => "Unknown"
            };
        }

        public static string GetStatusDisplayName(this string isActive)
        {
            return isActive switch
            {
                "A" => "Active",
                "D" => "Disabled",
                _ => "Unknown"
            };
        }
    }
}