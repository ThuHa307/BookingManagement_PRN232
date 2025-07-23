using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Post
{
    public class PricingLookupDto
    {
        public int TimeUnitId { get; set; }
        public int PackageTypeId { get; set; }
        public int DurationValue { get; set; }
    }
}