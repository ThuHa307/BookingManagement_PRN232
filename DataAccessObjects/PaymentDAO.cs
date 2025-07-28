using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;
using DataAccessObjects.DB;
using RentNest.Infrastructure.DataAccess;

namespace DataAccessObjects
{
    public class PaymentDAO : BaseDAO<Payment>
    {
        public PaymentDAO(RentNestSystemContext context) : base(context) { }
    }
}