using BusinessObjects.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IMessageService
    {
        Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId);
        Task AddMessageAsync(Message message);
    }
} 