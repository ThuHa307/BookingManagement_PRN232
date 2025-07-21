namespace BusinessObjects.Dtos
{
    public class SearchRequestDto
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
} 