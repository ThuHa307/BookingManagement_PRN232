namespace WebMVC.Models
{
    public class RoleDistributionItem
    {
        public string Role { get; set; }
        public int Count { get; set; }
    }

    public class AuthProviderDistributionItem
    {
        public string Provider { get; set; }
        public int Count { get; set; }
    }

    public class AccountStatistics
    {
        public int TotalAccounts { get; set; }
        public int ActiveAccounts { get; set; }
        public int InactiveAccounts { get; set; }
        public int OnlineAccounts { get; set; }
        public int TwoFactorEnabledAccounts { get; set; }
        public IEnumerable<RoleDistributionItem> RoleDistribution { get; set; }
        public IEnumerable<AuthProviderDistributionItem> AuthProviderDistribution { get; set; }
    }
}
