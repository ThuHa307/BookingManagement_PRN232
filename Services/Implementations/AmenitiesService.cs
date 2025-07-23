using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using Repositories.Interfaces;
using Services.Interfaces;

namespace Services.Implementations
{
    public class AmenitiesService : IAmenitiesSerivce
    {
        private readonly IAmenitiesRepository _amenitiesRepository;

        public AmenitiesService(IAmenitiesRepository amenitiesRepository)
        {
            _amenitiesRepository = amenitiesRepository;
        }
        public async Task<IEnumerable<Amenity>> GetAll()
        {
            return await _amenitiesRepository.GetAll();
        }
    }
}