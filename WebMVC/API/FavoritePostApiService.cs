using BusinessObjects.Dtos;
using System.Net.Http;
using System.Net.Http.Json;

namespace WebMVC.API
{
    public class FavoritePostApiService
    {
        private readonly HttpClient _httpClient;

        public FavoritePostApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<FavoritePostDto>> GetByAccountIdAsync(int accountId)
        {
            return await _httpClient.GetFromJsonAsync<List<FavoritePostDto>>(
                $"/api/FavoritePost/account/{accountId}");
        }
        public async Task<bool> AddAsync(FavoritePostDto dto)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/FavoritePost/add", dto);
            return response.IsSuccessStatusCode;
        }


        public async Task<bool> DeleteAsync(int favoriteId)
        {
            var response = await _httpClient.DeleteAsync($"/api/FavoritePost/{favoriteId}");
            return response.IsSuccessStatusCode;
        }
    }
}
