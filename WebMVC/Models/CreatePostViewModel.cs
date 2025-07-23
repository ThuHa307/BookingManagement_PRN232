using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BusinessObjects.Domains;

namespace WebMVC.Models
{
    public class CreatePostViewModel
    {
        public IEnumerable<AccommodationType>? AccommodationTypes { get; set; }
        public IEnumerable<Amenity>? Amenities { get; set; }
        public IEnumerable<TimeUnitPackage>? TimeUnitPackages { get; set; }
        public IEnumerable<PackagePricing>? PackagePricings { get; set; }

        public string? AccountName;

        public string? PhoneNumber;
    }
}