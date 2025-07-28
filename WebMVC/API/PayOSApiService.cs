using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RentNest.Core.Model.PayOS;

namespace WebMVC.API
{
    public class PayOSApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PayOSApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _httpClient.BaseAddress = new Uri(_configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<PayOSResponseModel?> CreatePaymentLinkAsync(CreatePaymentLinkRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("api/payos/checkout/create", content);

            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PayOSResponseModel>();
        }
    }
}