﻿

using BusinessObjects.Domains;
using DataAccessObjects.DB;

namespace RentNest.Infrastructure.DataAccess
{
    public class AccommodationDetailDAO : BaseDAO<AccommodationDetail>
    {
        public AccommodationDetailDAO(RentNestSystemContext context) : base(context) { }
    }
}
