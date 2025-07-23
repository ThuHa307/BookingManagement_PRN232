using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using RentNest.Infrastructure.DataAccess;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class AccommodationTypeRepository : IAccommodationTypeRepository
    {
        private readonly AccommodationTypeDAO _accommodationTypeDAO;

        public AccommodationTypeRepository(AccommodationTypeDAO accommodationTypeDAO)
        {
            _accommodationTypeDAO = accommodationTypeDAO;
        }
        public async Task<IEnumerable<AccommodationType>> GetAllAsync()
        {
            return await _accommodationTypeDAO.GetAllAsync();
        }
    }
}