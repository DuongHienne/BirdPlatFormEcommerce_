using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbPost
{
    public int Id { get; set; }

    public int ShopId { get; set; }

    public int ProductId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? CreateBy { get; set; }

    public virtual TbProduct Product { get; set; } = null!;

    public virtual TbShop Shop { get; set; } = null!;
}
