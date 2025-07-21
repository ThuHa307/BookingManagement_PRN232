using System;
using System.Collections.Generic;

namespace BusinessObjects.Domains;

public partial class PostPackageType
{
    public int PackageTypeId { get; set; }

    public string PackageTypeName { get; set; } = null!;

    public int Priority { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<PackagePricing> PackagePricings { get; set; } = new List<PackagePricing>();
}
