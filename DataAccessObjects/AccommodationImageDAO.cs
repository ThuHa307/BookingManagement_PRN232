using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using DataAccessObjects.DB;

namespace RentNest.Infrastructure.DataAccess
{
    public class AccommodationImageDAO : BaseDAO<AccommodationImage>
    {
        public AccommodationImageDAO(RentNestSystemContext context) : base(context) { }
    }
}
