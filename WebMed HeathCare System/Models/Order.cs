using System;
using System.Collections.Generic;

namespace WebMed_HeathCare_System.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int PatientId { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string OrderStatus { get; set; } = null!;

    public int? PharmacistId { get; set; }

    public string ShippingAddress { get; set; } = null!;

    public string ShippingPhone { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Patient Patient { get; set; } = null!;

    public virtual User? Pharmacist { get; set; }
}
