using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbOrder
{
    public int OrderId { get; set; }

    public bool? Status { get; set; }

    public int UserId { get; set; }

    public string? Note { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime? OrderDate { get; set; }

    public int? PaymentId { get; set; }

    public int AddressId { get; set; }

    public int ShopId { get; set; }

    public virtual TbAddressReceive Address { get; set; } = null!;

    public virtual TbPayment? Payment { get; set; }

    public virtual TbShop Shop { get; set; } = null!;

    public virtual ICollection<TbOrderDetail> TbOrderDetails { get; set; } = new List<TbOrderDetail>();

    public virtual TbUser User { get; set; } = null!;
}
