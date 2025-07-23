using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;

namespace Repositories.Interfaces
{
    public interface IAmenitiesRepository
    {
        Task<IEnumerable<Amenity>> GetAll();
    }
}