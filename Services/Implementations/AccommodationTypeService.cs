using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class AccommodationTypeService : IAccommodationTypeService
    {
        private readonly IAccommodationTypeRepository _iAccommodationTypeRepository;

        public AccommodationTypeService(IAccommodationTypeRepository iAccommodationTypeRepository)
        {
            _iAccommodationTypeRepository = iAccommodationTypeRepository;
        }
        public async Task<IEnumerable<AccommodationType>> GetAllAsync()
        {
            return await _iAccommodationTypeRepository.GetAllAsync();
        }
    }
}