using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Payment
{
    public string PaymentId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? PaymentDescription { get; set; }

    public decimal? Amount { get; set; }

    public int? PointsEarned { get; set; }

    public string? PaymentStatus { get; set; }

    public DateTime PaymentDate { get; set; }

    public virtual Account? User { get; set; }
}
