using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        private readonly RentNestSystemContext _context;
        public ConversationRepository(RentNestSystemContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Conversation?> GetExistingConversationAsync(int senderId, int receiverId, int? postId)
        {
            return await _context.Conversations
                .FirstOrDefaultAsync(c => c.SenderId == senderId && c.ReceiverId == receiverId && c.PostId == postId);
        }

        public async Task<List<Conversation>> GetByUserIdAsync(int userId)
        {
            return await _context.Conversations
                .Include(c => c.Sender).ThenInclude(u => u.UserProfile)
                .Include(c => c.Receiver).ThenInclude(u => u.UserProfile)
                .Include(c => c.Post).ThenInclude(p => p.Accommodation)
                .Where(c => c.SenderId == userId || c.ReceiverId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<Conversation> AddIfNotExistsAsync(int senderId, int receiverId, int? postId)
        {
            var existing = await GetExistingConversationAsync(senderId, receiverId, postId);
            if (existing != null) return existing;
            var conversation = new Conversation
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                PostId = postId,
                StartedAt = System.DateTime.Now,
                UpdatedAt = System.DateTime.Now
            };
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
            return conversation;
        }

        public async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Messages)
                .Include(c => c.Sender).ThenInclude(u => u.UserProfile)
                .Include(c => c.Receiver).ThenInclude(u => u.UserProfile)
                .Include(c => c.Post).ThenInclude(p => p.Accommodation)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        }
    }
} 