using System;
using System.Collections.Generic;

namespace BusinessObjects.Domains;

public partial class AccommodationImage
{
    public int ImageId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int AccommodationId { get; set; }

    public virtual Accommodation Accommodation { get; set; } = null!;
}
