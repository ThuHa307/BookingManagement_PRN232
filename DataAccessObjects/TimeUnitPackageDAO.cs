using BusinessObjects.Domains;
using DataAccessObjects.DB;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RentNest.Infrastructure.DataAccess
{
    public class TimeUnitPackageDAO : BaseDAO<TimeUnitPackage>
    {
        public TimeUnitPackageDAO(RentNestSystemContext context) : base(context) { }
        public async Task<TimeUnitPackage> GetTimeUnitById(int timeUnitId)
        {
            return await _context.TimeUnitPackages.FirstOrDefaultAsync(t => t.TimeUnitId == timeUnitId);
        }
    }
}
