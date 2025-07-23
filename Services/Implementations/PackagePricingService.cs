using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Post;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class PackagePricingService : IPackagePricingService
    {
        private readonly IPackagePricingRepository _packagePricingRepository;
        public PackagePricingService(IPackagePricingRepository packagePricingRepository)
        {
            _packagePricingRepository = packagePricingRepository;
        }

        public async Task<IEnumerable<PackagePricing>> GetAllPackageOptions()
        {
            return await _packagePricingRepository.GetAllPackageOptions();
        }

        public async Task<List<DurationPriceDto>> GetDurationsAndPrices(int timeUnitId, int packageTypeId)
        {
            return await _packagePricingRepository.GetDurationsAndPrices(timeUnitId, packageTypeId);
        }

        public async Task<List<PackageTypeDto>> GetPackageTypes(int timeUnitId)
        {
            return await _packagePricingRepository.GetPackageTypes(timeUnitId);
        }

        public async Task<List<PostPackageType>> GetPackageTypesByTimeUnit(int timeUnitId)
        {
            return await _packagePricingRepository.GetPackageTypesByTimeUnit(timeUnitId);
        }

        public async Task<int?> GetPricingIdAsync(int timeUnitId, int packageTypeId, int durationValue)
        {
            return await _packagePricingRepository.GetPricingIdAsync(timeUnitId, packageTypeId, durationValue);
        }
    }
}