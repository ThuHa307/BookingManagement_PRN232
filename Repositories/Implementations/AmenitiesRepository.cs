using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using RentNest.Infrastructure.DataAccess;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class AmenitiesRepository : IAmenitiesRepository
    {
        private readonly AmenitiesDAO _amenitiesDAO;

        public AmenitiesRepository(AmenitiesDAO amenitiesDAO)
        {
            _amenitiesDAO = amenitiesDAO;
        }
        public async Task<IEnumerable<Amenity>> GetAll()
        {
            return await _amenitiesDAO.GetAllAsync();
        }
    }
}