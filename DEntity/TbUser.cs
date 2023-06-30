using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbUser
{
    public DateTime? Dob { get; set; }

    public string? Gender { get; set; }

    public int UserId { get; set; }

    public string? Email { get; set; }

    public string Name { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public DateTime? UpdateDate { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? Avatar { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public bool? IsShop { get; set; }

    public virtual TbRole Role { get; set; } = null!;

    public virtual ICollection<TbAddressReceive> TbAddressReceives { get; set; } = new List<TbAddressReceive>();

    public virtual ICollection<TbCart> TbCarts { get; set; } = new List<TbCart>();

    public virtual ICollection<TbFeedback> TbFeedbacks { get; set; } = new List<TbFeedback>();

    public virtual ICollection<TbOrder> TbOrders { get; set; } = new List<TbOrder>();

    public virtual ICollection<TbPayment> TbPayments { get; set; } = new List<TbPayment>();

    public virtual ICollection<TbShop> TbShops { get; set; } = new List<TbShop>();

    public virtual ICollection<TbToken> TbTokens { get; set; } = new List<TbToken>();

    public virtual ICollection<TbWishList> TbWishLists { get; set; } = new List<TbWishList>();
}
