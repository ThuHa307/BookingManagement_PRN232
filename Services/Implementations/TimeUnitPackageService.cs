using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class TimeUnitPackageService : ITimeUnitPackageService
    {
        private readonly ITimeUnitPackageRepository _timeUnitPackageRepository;

        public TimeUnitPackageService(ITimeUnitPackageRepository timeUnitPackageRepository)
        {
            _timeUnitPackageRepository = timeUnitPackageRepository;
        }
        public async Task<IEnumerable<TimeUnitPackage>> GetAll()
        {
            return await _timeUnitPackageRepository.GetAll();
        }
    }
}