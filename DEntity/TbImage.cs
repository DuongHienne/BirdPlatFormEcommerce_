using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? VideoPath { get; set; }

    public string? ImagePath { get; set; }

    public string? Caption { get; set; }

    public bool? IsDefault { get; set; }

    public DateTime? CreateDate { get; set; }

    public int? SortOrder { get; set; }

    public long? FileSize { get; set; }

    public virtual TbProduct Product { get; set; } = null!;
}
