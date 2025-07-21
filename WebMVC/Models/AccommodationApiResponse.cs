using BusinessObjects.Dtos;

namespace WebMVC.Models
{
    public class AccommodationApiResponse
    {
        public List<AccommodationDto> Data { get; set; } = new List<AccommodationDto>();
        public bool HasSearched { get; set; }
        public FilterData Filters { get; set; } = new FilterData();
    }

    public class FilterData
    {
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
        public double? Area { get; set; }
        public decimal? MinMoney { get; set; }
        public decimal? MaxMoney { get; set; }
        public string? RoomType { get; set; }
        public string? FurnitureStatus { get; set; }
        public int? BedroomCount { get; set; }
        public int? BathroomCount { get; set; }
    }
} 