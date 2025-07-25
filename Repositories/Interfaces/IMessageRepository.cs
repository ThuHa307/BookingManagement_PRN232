using BusinessObjects.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<List<Message>> GetMessagesByConversationIdAsync(int conversationId);
        Task AddMessageAsync(Message message);
    }
} 