using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbShop
{
    public int ShopId { get; set; }

    public int? Rate { get; set; }

    public string ShopName { get; set; } = null!;

    public string? Description { get; set; }

    public int? UserId { get; set; }

    public bool? IsVerified { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<TbOrder> TbOrders { get; set; } = new List<TbOrder>();

    public virtual ICollection<TbPost> TbPosts { get; set; } = new List<TbPost>();

    public virtual ICollection<TbProduct> TbProducts { get; set; } = new List<TbProduct>();

    public virtual TbUser? User { get; set; }
}
