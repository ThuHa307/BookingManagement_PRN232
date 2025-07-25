using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using Repositories.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories.Implementations
{
    public class QuickReplyTemplateRepository : GenericRepository<QuickReplyTemplate>, IQuickReplyTemplateRepository
    {
        private readonly RentNestSystemContext _context;
        public QuickReplyTemplateRepository(RentNestSystemContext context) : base(context)
        {
            _context = context;
        }

        public async Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAsync(string role)
        {
            return await _context.QuickReplyTemplates
                .Where(q => q.TargetRole == role)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task AddQuickReplyAsync(string content, string role, int accountId)
        {
            var quickReply = new QuickReplyTemplate
            {
                Message = content,
                TargetRole = role,
                AccountId = accountId,
                CreatedAt = System.DateTime.Now
            };
            await _context.QuickReplyTemplates.AddAsync(quickReply);
            await _context.SaveChangesAsync();
        }

        public async Task<List<QuickReplyTemplate>> GetQuickRepliesByRoleAndAccountAsync(string role, int accountId)
        {
            return await _context.QuickReplyTemplates
                .Where(q => q.TargetRole == role && (q.AccountId == accountId || q.AccountId == null))
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }
    }
} 