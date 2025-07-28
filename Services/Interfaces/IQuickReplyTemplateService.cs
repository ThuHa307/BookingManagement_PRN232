using BusinessObjects.Domains;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface IQuickReplyTemplateService
    {
        Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAsync(string role);
        Task AddQuickReplyAsync(string content, string role, int accountId);
        Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAndAccountAsync(string role, int accountId);
    }
} 