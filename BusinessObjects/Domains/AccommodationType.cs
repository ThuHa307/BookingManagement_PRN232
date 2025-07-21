using System;
using System.Collections.Generic;

namespace BusinessObjects.Domains;

public partial class AccommodationType
{
    public int TypeId { get; set; }

    public string TypeName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
}
