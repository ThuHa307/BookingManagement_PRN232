using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RentNest.Core.Model.PayOS;

namespace WebMVC.API
{
    public interface IPaymentPayOSApiService
    {
        Task<bool> HandlePaymentSuccessAsync(int amount, string transactionId, int postId);
        Task<PayOSResponseModel?> HandlePaymentCancelAsync(int postId, int amount);
    }

    public class PaymentPayOSApiService : IPaymentPayOSApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PaymentPayOSApiService> _logger;

        public PaymentPayOSApiService(HttpClient httpClient,
                                      IConfiguration configuration,
                                      ILogger<PaymentPayOSApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _httpClient.BaseAddress = new Uri(configuration["ApiSettings:ApiBaseUrl"]!);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<bool> HandlePaymentSuccessAsync(int amount, string transactionId, int postId)
        {
            try
            {
                var txn = Uri.EscapeDataString(transactionId ?? string.Empty);
                var url = $"api/PaymentPayOS/success?amount={amount}&transactionId={txn}&postId={postId}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HandlePaymentSuccessAsync failed. StatusCode={StatusCode}", response.StatusCode);
                    return false;
                }

                // Nếu backend trả { success = true }, có thể parse để chắc chắn:
                // var payload = await response.Content.ReadFromJsonAsync<YourSuccessDto>();
                // return payload?.success ?? false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandlePaymentSuccessAsync exception");
                return false;
            }
        }

        public async Task<PayOSResponseModel?> HandlePaymentCancelAsync(int postId, int amount)
        {
            try
            {
                var url = $"api/PaymentPayOS/cancel?postId={postId}&amount={amount}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("HandlePaymentCancelAsync failed. StatusCode={StatusCode}", response.StatusCode);
                    return null;
                }

                return await response.Content.ReadFromJsonAsync<PayOSResponseModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HandlePaymentCancelAsync exception");
                return null;
            }
        }
    }
}
