using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbPayment
{
    public int PaymentId { get; set; }

    public int UserId { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? PaymentDate { get; set; }

    public decimal? Amount { get; set; }

    public virtual ICollection<TbOrder> TbOrders { get; set; } = new List<TbOrder>();

    public virtual TbUser User { get; set; } = null!;
}
