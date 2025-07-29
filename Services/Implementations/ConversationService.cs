using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        public ConversationService(IConversationRepository conversationRepository)
        {
            _conversationRepository = conversationRepository;
        }

        public async Task<Conversation?> GetExistingConversationAsync(int senderId, int receiverId, int? postId)
            => await _conversationRepository.GetExistingConversationAsync(senderId, receiverId, postId);

        public async Task<List<Conversation>> GetByUserIdAsync(int userId)
            => await _conversationRepository.GetByUserIdAsync(userId);

        public async Task<Conversation> AddIfNotExistsAsync(int senderId, int receiverId, int? postId)
            => await _conversationRepository.AddIfNotExistsAsync(senderId, receiverId, postId);

        public async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId)
            => await _conversationRepository.GetConversationWithMessagesAsync(conversationId);
    }
} 