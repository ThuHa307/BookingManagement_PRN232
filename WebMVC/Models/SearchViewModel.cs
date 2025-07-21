namespace WebMVC.Models
{
    public class SearchViewModel
    {
        public string ProvinceName { get; set; }
        public string DistrictName { get; set; }
        public string? WardName { get; set; }
        public double? Area { get; set; }
        public decimal? MinMoney { get; set; }
        public decimal? MaxMoney { get; set; }
        public string ProvinceId { get; set; }
        public string DistrictId { get; set; }
        public string? WardId { get; set; }
    }

    public class SearchApiResponse
    {
        public List<BusinessObjects.Dtos.AccommodationDto> Data { get; set; } = new List<BusinessObjects.Dtos.AccommodationDto>();
        public bool HasSearched { get; set; }
        public SearchParams SearchParams { get; set; } = new SearchParams();
    }

    public class SearchParams
    {
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public string? ProvinceId { get; set; }
        public string? DistrictId { get; set; }
        public string? WardId { get; set; }
        public double? Area { get; set; }
        public decimal? MinMoney { get; set; }
        public decimal? MaxMoney { get; set; }
    }
} 