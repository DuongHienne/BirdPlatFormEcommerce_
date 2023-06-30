using System;
using System.Collections.Generic;

namespace BirdPlatFormEcommerce.DEntity;

public partial class TbFeedbackImage
{
    public int FbImgId { get; set; }

    public int? FeedbackId { get; set; }

    public string? ImagePath { get; set; }

    public long? FileSize { get; set; }

    public virtual TbFeedback? Feedback { get; set; }
}
