using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using BusinessObjects.Dtos.Post;

namespace Services.Interfaces
{
    public interface IPackagePricingService
    {
        Task<IEnumerable<PackagePricing>> GetAllPackageOptions();
        Task<List<PostPackageType>> GetPackageTypesByTimeUnit(int timeUnitId);
        Task<List<DurationPriceDto>> GetDurationsAndPrices(int timeUnitId, int packageTypeId);
        Task<List<PackageTypeDto>> GetPackageTypes(int timeUnitId);
        Task<int?> GetPricingIdAsync(int timeUnitId, int packageTypeId, int durationValue);
    }
}