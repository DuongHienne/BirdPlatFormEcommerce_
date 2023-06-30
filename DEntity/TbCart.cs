using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbCart
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public string? ShopName { get; set; }

    public int ProductId { get; set; }

    public virtual TbProduct Product { get; set; } = null!;

    public virtual TbUser User { get; set; } = null!;
}
