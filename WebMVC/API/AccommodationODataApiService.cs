using BusinessObjects.Dtos;
using System.Text;
using System.Text.Json;
using WebMVC.Models;
using RentNest.Core.UtilHelper;

namespace WebMVC.API
{
    public class AccommodationODataApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public AccommodationODataApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
        }

        /// <summary>
        /// Lấy danh sách accommodations với OData query
        /// </summary>
        public async Task<List<AccommodationDto>> GetAccommodationsAsync(string? odataQuery = null)
        {
            try
            {
                var apiUrl = "/api/Accommodations/odata";
                if (!string.IsNullOrEmpty(odataQuery))
                {
                    apiUrl += $"?{odataQuery}";
                }

                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadFromJsonAsync<List<AccommodationDto>>();

                return jsonContent;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching accommodations with OData: {ex.Message}");
                return new List<AccommodationDto>();
            }
        }

        /// <summary>
        /// Mã hóa giá trị string cho OData (chỉ escape single quotes, không URL encode)
        /// </summary>
        private string EscapeODataStringValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Chỉ escape single quotes bằng cách nhân đôi chúng (theo chuẩn OData)
            // Không URL encode vì OData server sẽ tự xử lý
            return value.Replace("'", "''");
        }

        /// <summary>
        /// Tạo OData filter query từ các tham số tìm kiếm
        /// </summary>
        public string BuildODataFilterQuery(
            string? roomType = null,
            string? furnitureStatus = null,
            int? bedroomCount = null,
            int? bathroomCount = null,
            string? provinceName = null,
            string? districtName = null,
            string? wardName = null,
            double? area = null,
            decimal? minMoney = null,
            decimal? maxMoney = null)
        {
            var filters = new List<string>();

            // Filter theo tiêu đề (roomType)
            if (!string.IsNullOrEmpty(roomType))
            {
                var escapedRoomType = EscapeODataStringValue(roomType);
                filters.Add($"contains(tolower(Title), tolower('{escapedRoomType}'))");
            }

            // Filter theo trạng thái (furnitureStatus)
            if (!string.IsNullOrEmpty(furnitureStatus))
            {
                var escapedFurnitureStatus = EscapeODataStringValue(furnitureStatus);
                filters.Add($"Status eq '{escapedFurnitureStatus}'");
            }

            // Filter theo số phòng ngủ
            if (bedroomCount.HasValue)
            {
                filters.Add($"BedroomCount eq {bedroomCount.Value}");
            }

            // Filter theo số phòng tắm
            if (bathroomCount.HasValue)
            {
                filters.Add($"BathroomCount eq {bathroomCount.Value}");
            }

            // Filter theo tỉnh/thành
            if (!string.IsNullOrEmpty(provinceName))
            {
                // Chuẩn hóa tên tỉnh/thành phố như logic cũ
                var normalizedProvinceName = ProvinceNameNormalizer.Normalize(provinceName);
                var escapedProvinceName = EscapeODataStringValue(normalizedProvinceName);
                filters.Add($"contains(tolower(ProvinceName), tolower('{escapedProvinceName}'))");
            }

            // Filter theo quận/huyện
            if (!string.IsNullOrEmpty(districtName))
            {
                var escapedDistrictName = EscapeODataStringValue(districtName);
                filters.Add($"contains(tolower(DistrictName), tolower('{escapedDistrictName}'))");
            }

            // Filter theo phường/xã
            if (!string.IsNullOrEmpty(wardName))
            {
                var escapedWardName = EscapeODataStringValue(wardName);
                filters.Add($"contains(tolower(WardName), tolower('{escapedWardName}'))");
            }

            // Filter theo diện tích
            if (area.HasValue)
            {
                filters.Add($"Area ge {area.Value}");
            }

            // Filter theo khoảng giá
            if (minMoney.HasValue)
            {
                filters.Add($"Price ge {minMoney.Value}");
            }

            if (maxMoney.HasValue)
            {
                filters.Add($"Price le {maxMoney.Value}");
            }

            return string.Join(" and ", filters);
        }

        /// <summary>
        /// Tạo OData query hoàn chỉnh với filter, select, orderby, top, skip
        /// </summary>
        public string BuildCompleteODataQuery(
            string? filter = null,
            string? select = null,
            string? orderBy = null,
            int? top = null,
            int? skip = null,
            bool count = false)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(filter))
            {
                // Không mã hóa toàn bộ filter vì các giá trị string đã được mã hóa trong BuildODataFilterQuery
                queryParams.Add($"$filter={filter}");
            }

            if (!string.IsNullOrEmpty(select))
            {
                queryParams.Add($"$select={select}");
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                queryParams.Add($"$orderby={orderBy}");
            }

            if (top.HasValue)
            {
                queryParams.Add($"$top={top.Value}");
            }

            if (skip.HasValue)
            {
                queryParams.Add($"$skip={skip.Value}");
            }

            if (count)
            {
                queryParams.Add("$count=true");
            }

            return string.Join("&", queryParams);
        }

        /// <summary>
        /// Lấy danh sách accommodations với tìm kiếm nâng cao
        /// </summary>
        public async Task<List<AccommodationDto>> SearchAccommodationsAsync(
            string? provinceName = null,
            string? districtName = null,
            string? wardName = null,
            double? area = null,
            decimal? minMoney = null,
            decimal? maxMoney = null,
            string? orderBy = null,
            int? top = null)
        {
            var filter = BuildODataFilterQuery(
                provinceName: provinceName,
                districtName: districtName,
                wardName: wardName,
                area: area,
                minMoney: minMoney,
                maxMoney: maxMoney);

            var completeQuery = BuildCompleteODataQuery(
                filter: filter,
                orderBy: orderBy,
                top: top);

            return await GetAccommodationsAsync(completeQuery);
        }
    }
} 