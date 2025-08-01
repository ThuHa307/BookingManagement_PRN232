using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure;
using BusinessObjects.Dtos.ChatBot;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RentNest.Core.Configs;
using RentNest.Core.Consts;
using Services.Interfaces;
using Azure.AI.OpenAI;
using OpenAI.Chat;
namespace Services.Implementations
{
    public class AzureOpenAIService : IAzureOpenAIService
    {
        private readonly AzureOpenAISettings _setting;
        private readonly IPostService _postService;
        private readonly IMemoryCache _cache;

        public AzureOpenAIService(IOptions<AzureOpenAISettings> setting, IPostService postService, IMemoryCache cache)
        {
            _setting = setting.Value;
            _postService = postService;
            _cache = cache;
        }
        public async Task<string> GenerateDataPost(PostDataAIDto model)
        {
            var jsonData = JsonSerializer.Serialize(model);
            var style = model.AiStyle;

            var promptTemplate = PromptAI.Prompt;
            var promptText = promptTemplate.Replace("{json_data}", jsonData)
                                           .Replace("{style}", style);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(promptText)
            };

            var client = new AzureOpenAIClient(
                new Uri(_setting.Endpoint),
                new AzureKeyCredential(_setting.ApiKey));
            var chatClient = client.GetChatClient(_setting.DeploymentName);

            var response = await chatClient.CompleteChatAsync(messages);
            var result = response.Value.Content[0].Text;

            return result;
        }

        public async Task<string> ChatWithAIAsync(string userMessage)
        {
            if (_cache.TryGetValue(userMessage, out string cachedResponse))
            {
                return cachedResponse;
            }

            var allPosts = await _postService.GetAllPostsWithAccommodation();
            var sb = new StringBuilder();

            foreach (var post in allPosts)
            {
                sb.Append($"Tiêu đề: {post.Title} - Địa chỉ: {post.Accommodation.Address} - Giá: {post.Accommodation.Price:N0} VNĐ - " +
                          $"Link: <a href=\"/chi-tiet/{post.Accommodation.AccommodationId}\" style=\"color: #fff !important; text-decoration: underline\">Xem chi tiết</a><br>");
            }


            var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(@"
                        Bạn là một trợ lý AI thân thiện giúp người dùng tìm trọ (dành cho khách hoặc người thuê) hoặc đăng tin (dành cho chủ trọ) cho trang web BlueHouse. 
                        Khi người dùng hỏi về địa điểm hoặc nhu cầu thuê trọ, hãy tìm trong danh sách bài viết đã được cung cấp {allPosts}.
                        Nếu có bài đăng phù hợp theo dữ liệu ${allPosts}, hãy trả lời rõ ràng và ghi rõ đường dẫn bài đăng - hãy chèn liên kết bằng thẻ <a>.
                        KHÔNG DÙNG BẤT KỂ TỪ IN ĐẬM NÀO.
                        NẾU TRẢ VỀ DANH SÁCH BÀI ĐĂNG HÃY XUỐNG HÀNG FORMAT CHO ĐẸP (THÊM THẺ <BR> CUỐI THẺ <A>)
                    "),
                    new AssistantChatMessage("Dưới đây là danh sách bài đăng hiện có:<br>" + sb.ToString()),
                    new UserChatMessage(userMessage)
                };

            var client = new AzureOpenAIClient(
                new Uri(_setting.Endpoint),
                new AzureKeyCredential(_setting.ApiKey));
            var chatClient = client.GetChatClient(_setting.DeploymentName);

            var response = await chatClient.CompleteChatAsync(messages);
            var result = response.Value.Content[0].Text;

            _cache.Set(userMessage, result, TimeSpan.FromMinutes(30));

            return result;
        }
        public async Task<string> ScoreCommentAsync(CommentScoreDto model)
        {
            var promptTemplate = PromptAI.PromptScoreComment;
            var promptText = promptTemplate.Replace("{post_content}", model.PostContent)
                                           .Replace("{comment_content}", model.CommentContent);

            var messages = new List<ChatMessage>
    {
        new SystemChatMessage(promptText)
    };

            var client = new AzureOpenAIClient(
                new Uri(_setting.Endpoint),
                new AzureKeyCredential(_setting.ApiKey));
            var chatClient = client.GetChatClient(_setting.DeploymentName);

            var response = await chatClient.CompleteChatAsync(messages);
            var result = response.Value.Content[0].Text;

            return result.Trim(); // chỉ trả về số điểm
        }
        public async Task<double> ScoreAverageCommentAsync(PostWithCommentsDto model)
        {
            var totalScore = 0;
            var validCount = 0;

            foreach (var comment in model.CommentContents)
            {
                var scoreDto = new CommentScoreDto
                {
                    PostContent = model.PostContent,
                    CommentContent = comment
                };

                var scoreText = await ScoreCommentAsync(scoreDto);

                if (int.TryParse(scoreText, out int score))
                {
                    totalScore += score;
                    validCount++;
                }
            }

            if (validCount == 0) return 0;
            return Math.Round((double)totalScore / validCount, 2);
        }




    }
}