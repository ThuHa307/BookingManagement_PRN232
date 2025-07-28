using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Threading.Tasks;
using BusinessObjects.Consts;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/chatroom")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = UserRoles.User + "," + UserRoles.Landlord)]
    public class ChatRoomController : ControllerBase
    {
        private readonly IConversationService _conversationService;
        private readonly IQuickReplyTemplateService _quickReplyTemplateService;
        private readonly IAccountService _accountService;
        public ChatRoomController(IConversationService conversationService, IQuickReplyTemplateService quickReplyTemplateService, IAccountService accountService)
        {
            _conversationService = conversationService;
            _quickReplyTemplateService = quickReplyTemplateService;
            _accountService = accountService;
        }

        [HttpGet("conversations")] // Lấy danh sách hội thoại của user
        public async Task<IActionResult> GetConversations()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();
            var conversations = await _conversationService.GetByUserIdAsync(userId);
            var dtos = conversations.Select(c => new ConversationDto
            {
                ConversationId = c.ConversationId,
                SenderId = c.SenderId,
                ReceiverId = c.ReceiverId,
                PostId = c.PostId,
                PostTitle = c.Post?.Title,
                Sender = c.Sender == null ? null : new UserDto
                {
                    AccountId = c.Sender.AccountId,
                    FirstName = c.Sender.UserProfile?.FirstName,
                    LastName = c.Sender.UserProfile?.LastName,
                    AvatarUrl = c.Sender.UserProfile?.AvatarUrl,
                    IsOnline = c.Sender.IsOnline
                },
                Receiver = c.Receiver == null ? null : new UserDto
                {
                    AccountId = c.Receiver.AccountId,
                    FirstName = c.Receiver.UserProfile?.FirstName,
                    LastName = c.Receiver.UserProfile?.LastName,
                    AvatarUrl = c.Receiver.UserProfile?.AvatarUrl,
                    IsOnline = c.Receiver.IsOnline
                },
                Post = c.Post == null ? null : new PostDto
                {
                    PostId = c.Post.PostId,
                    Title = c.Post.Title,
                    Price = c.Post.Accommodation?.Price,
                    ImageUrl = c.Post.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl
                },
                Messages = new List<MessageDto>() // Không trả về messages ở danh sách
            }).ToList();
            return Ok(dtos);
        }

        [HttpGet("detail/{id}")] // Lấy chi tiết hội thoại
        public async Task<IActionResult> GetConversationDetail(int id)
        {
            var c = await _conversationService.GetConversationWithMessagesAsync(id);
            if (c == null) return NotFound();
            var dto = new ConversationDto
            {
                ConversationId = c.ConversationId,
                SenderId = c.SenderId,
                ReceiverId = c.ReceiverId,
                PostId = c.PostId,
                PostTitle = c.Post?.Title,
                Sender = c.Sender == null ? null : new UserDto
                {
                    AccountId = c.Sender.AccountId,
                    FirstName = c.Sender.UserProfile?.FirstName,
                    LastName = c.Sender.UserProfile?.LastName,
                    AvatarUrl = c.Sender.UserProfile?.AvatarUrl,
                    IsOnline = c.Sender.IsOnline
                },
                Receiver = c.Receiver == null ? null : new UserDto
                {
                    AccountId = c.Receiver.AccountId,
                    FirstName = c.Receiver.UserProfile?.FirstName,
                    LastName = c.Receiver.UserProfile?.LastName,
                    AvatarUrl = c.Receiver.UserProfile?.AvatarUrl,
                    IsOnline = c.Receiver.IsOnline
                },
                Post = c.Post == null ? null : new PostDto
                {
                    PostId = c.Post.PostId,
                    Title = c.Post.Title,
                    Price = c.Post.Accommodation?.Price,
                    ImageUrl = c.Post.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl
                },
                Messages = c.Messages?.Select(m => new MessageDto
                {
                    MessageId = m.MessageId,
                    SenderId = m.SenderId,
                    Content = m.Content,
                    ImageUrl = m.ImageUrl,
                    SentAt = m.SentAt
                }).ToList() ?? new List<MessageDto>()
            };
            return Ok(dto);
        }

        [HttpPost("quick-message")] // Thêm tin nhắn nhanh
        public async Task<IActionResult> AddQuickMessage([FromBody] QuickReplyDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();
            var user = await _accountService.GetAccountByIdAsync(userId);
            await _quickReplyTemplateService.AddQuickReplyAsync(dto.Content, user.Role, userId);
            return Ok(new { message = "Đã thêm tin nhắn nhanh" });
        }

        [HttpPost("start")] // Bắt đầu hội thoại mới
        public async Task<IActionResult> StartConversation([FromBody] StartConversationDto dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int senderId))
                return Unauthorized();
            var c = await _conversationService.AddIfNotExistsAsync(senderId, dto.ReceiverId, dto.PostId);
            var conversationDto = new ConversationDto
            {
                ConversationId = c.ConversationId,
                SenderId = c.SenderId,
                ReceiverId = c.ReceiverId,
                PostId = c.PostId,
                PostTitle = c.Post?.Title,
                Sender = c.Sender == null ? null : new UserDto
                {
                    AccountId = c.Sender.AccountId,
                    FirstName = c.Sender.UserProfile?.FirstName,
                    LastName = c.Sender.UserProfile?.LastName,
                    AvatarUrl = c.Sender.UserProfile?.AvatarUrl,
                    IsOnline = c.Sender.IsOnline
                },
                Receiver = c.Receiver == null ? null : new UserDto
                {
                    AccountId = c.Receiver.AccountId,
                    FirstName = c.Receiver.UserProfile?.FirstName,
                    LastName = c.Receiver.UserProfile?.LastName,
                    AvatarUrl = c.Receiver.UserProfile?.AvatarUrl,
                    IsOnline = c.Receiver.IsOnline
                },
                Post = c.Post == null ? null : new PostDto
                {
                    PostId = c.Post.PostId,
                    Title = c.Post.Title,
                    Price = c.Post.Accommodation?.Price,
                    ImageUrl = c.Post.Accommodation?.AccommodationImages?.FirstOrDefault()?.ImageUrl
                },
                Messages = new List<MessageDto>() // Không trả về messages ở đây
            };
            return Ok(conversationDto);
        }

        [HttpGet("quick-replies")] // Lấy danh sách quick reply theo role và account
        public async Task<IActionResult> GetQuickReplies()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
                return Unauthorized();
            var user = await _accountService.GetAccountByIdAsync(userId);
            var quickReplies = await _quickReplyTemplateService.GetQuickRepliesByRoleAndAccountAsync(user.Role, userId);
            var dtos = quickReplies.Select(q => new QuickReplyDto
            {
                TemplateId = q.TemplateId,
                Content = q.Message // hoặc q.Content nếu DB dùng tên này
            }).ToList();
            return Ok(dtos);
        }
    }

    public class QuickReplyDto
    {
        public int TemplateId { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class StartConversationDto
    {
        public int ReceiverId { get; set; }
        public int? PostId { get; set; }
    }

    public class ConversationDto
    {
        public int ConversationId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public int? PostId { get; set; }
        public string? PostTitle { get; set; }
        public UserDto Sender { get; set; }
        public UserDto Receiver { get; set; }
        public PostDto? Post { get; set; }
        public List<MessageDto> Messages { get; set; }
    }
    public class UserDto
    {
        public int AccountId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool? IsOnline { get; set; }
    }
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string? Content { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? SentAt { get; set; }
    }
    public class PostDto
    {
        public int PostId { get; set; }
        public string? Title { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
    }
} 