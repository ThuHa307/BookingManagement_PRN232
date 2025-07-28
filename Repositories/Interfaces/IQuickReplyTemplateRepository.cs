using BusinessObjects.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories.Interfaces
{
    public interface IQuickReplyTemplateRepository : IGenericRepository<QuickReplyTemplate>
    {
        Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAsync(string role);
        Task AddQuickReplyAsync(string content, string role, int accountId);
        Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAndAccountAsync(string role, int accountId);
    }
} 