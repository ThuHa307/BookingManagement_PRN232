using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessObjects.Dtos.Post
{
    public class DurationPriceDto
    {
        public int DurationValue { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string TimeUnitName { get; set; }
    }
}