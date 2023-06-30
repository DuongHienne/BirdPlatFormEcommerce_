using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbProductCategory
{
    public string CateId { get; set; } = null!;

    public string? CateName { get; set; }

    public virtual ICollection<TbProduct> TbProducts { get; set; } = new List<TbProduct>();
}
