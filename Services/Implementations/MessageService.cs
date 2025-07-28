using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        public MessageService(IMessageRepository messageRepository)
        {
            _messageRepository = messageRepository;
        }

        public async Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId)
            => await _messageRepository.GetMessagesByConversationIdAsync(conversationId);

        public async Task AddMessageAsync(Message message)
            => await _messageRepository.AddMessageAsync(message);
    }
} 