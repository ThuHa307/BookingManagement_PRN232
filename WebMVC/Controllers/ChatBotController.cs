using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using BusinessObjects.Dtos.ChatBot;

namespace WebMVC.Controllers
{
    [Route("chatbot")]
    public class ChatController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequestDto requestDto)
        {
            var baseUrl = _configuration["ApiSettings:ApiBaseUrl"]; // ví dụ http://localhost:5050
            var requestUrl = $"{baseUrl}/api/chatbot/ask";

            var content = new StringContent(
                JsonSerializer.Serialize(requestDto),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(requestUrl, content);
            var json = await response.Content.ReadAsStringAsync();

            return Content(json, "application/json");
        }
    }
}
