namespace WebMVC.Models
{
    public class DashboardViewModel
    {
        public BusinessObjects.Dtos.Common.PagedResultDto<BusinessObjects.Dtos.Account.AccountResponseDto> Accounts { get; set; }
        public AccountStatistics Statistics { get; set; }
    }
}
