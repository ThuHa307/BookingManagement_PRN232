using WebMVC.Models;

namespace WebMVC.API
{
    public interface IHomeApiService
    {
        Task<ApiResponse<string>> SendContactAsync(ContactFormViewModel model);
        Task<ContactInfo> GetContactInfoAsync();
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class ContactInfo
    {
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
    }
}
