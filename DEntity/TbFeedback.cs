using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbFeedback
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int? Rate { get; set; }

    public string? Detail { get; set; }

    public virtual TbProduct Product { get; set; } = null!;

    public virtual ICollection<TbFeedbackImage> TbFeedbackImages { get; set; } = new List<TbFeedbackImage>();

    public virtual TbUser User { get; set; } = null!;
}
