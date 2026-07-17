using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string PaymentType { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public string? TransactionReference { get; set; }

    public string PaymentStatus { get; set; } = null!;

    public int? AssociatedId { get; set; }

    public DateTime PaidAt { get; set; }

    public virtual User User { get; set; } = null!;
}
