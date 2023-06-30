using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbAddressReceive
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public string? Address { get; set; }

    public string? AddressDetail { get; set; }

    public string? Phone { get; set; }

    public string? NameRg { get; set; }

    public virtual ICollection<TbOrder> TbOrders { get; set; } = new List<TbOrder>();

    public virtual TbUser User { get; set; } = null!;
}
