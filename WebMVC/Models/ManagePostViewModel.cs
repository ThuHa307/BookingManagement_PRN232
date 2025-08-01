using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebMVC.Models
{
    public class ManagePostViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Address { get; set; }
        public decimal? Price { get; set; }
        public string Status { get; set; }
        public string ImageUrl { get; set; }
        public int? Area { get; set; }
        public int? BedroomCount { get; set; }
        public int? BathroomCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string DistrictName { get; set; }
        public string WardName { get; set; }
        public string ProvinceName { get; set; }
        public string PackageTypeName { get; set; }
        public string TimeUnitName { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? RejectionReason { get; set; }
        public string? AccountName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}