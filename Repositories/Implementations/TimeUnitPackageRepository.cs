using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using RentNest.Infrastructure.DataAccess;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class TimeUnitPackageRepository : ITimeUnitPackageRepository
    {
        private readonly TimeUnitPackageDAO _timeUnitPackageDAO;

        public TimeUnitPackageRepository(TimeUnitPackageDAO timeUnitPackageDAO)
        {
            _timeUnitPackageDAO = timeUnitPackageDAO;
        }
        public async Task<List<TimeUnitPackage>> GetAll()
        {
            return await _timeUnitPackageDAO.GetAllAsync();
        }
    }
}