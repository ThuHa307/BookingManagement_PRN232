using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Implementations
{
    public class QuickReplyTemplateService : IQuickReplyTemplateService
    {
        private readonly IQuickReplyTemplateRepository _quickReplyTemplateRepository;
        public QuickReplyTemplateService(IQuickReplyTemplateRepository quickReplyTemplateRepository)
        {
            _quickReplyTemplateRepository = quickReplyTemplateRepository;
        }

        public async Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAsync(string role)
            => await _quickReplyTemplateRepository.GetQuickRepliesByRoleAsync(role);

        public async Task AddQuickReplyAsync(string content, string role, int accountId)
            => await _quickReplyTemplateRepository.AddQuickReplyAsync(content, role, accountId);

        public async Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAndAccountAsync(string role, int accountId)
            => await _quickReplyTemplateRepository.GetQuickRepliesByRoleAndAccountAsync(role, accountId);
    }
} 