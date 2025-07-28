using BusinessObjects.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<Conversation?> GetExistingConversationAsync(int senderId, int receiverId, int? postId);
        Task<List<Conversation>> GetByUserIdAsync(int userId);
        Task<Conversation> AddIfNotExistsAsync(int senderId, int receiverId, int? postId);
        Task<Conversation?> GetConversationWithMessagesAsync(int conversationId);
    }
} 