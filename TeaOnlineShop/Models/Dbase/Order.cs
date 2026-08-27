using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TeaOnlineShop.Models.Dbase;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required, StringLength(50)]
    public string FirstName { get; set; } = null!;

    [Required, StringLength(50)]
    public string LastName { get; set; } = null!;

    [Required, StringLength(50)]
    public string Country { get; set; } = null!;

    [Required, StringLength(200)]
    public string Address { get; set; } = null!;

    [Required, StringLength(50)]
    public string City { get; set; } = null!;

    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = null!;

    [Required, Phone, StringLength(50)]
    public string Phone { get; set; } = null!;

    [StringLength(250)]
    public string? Comment { get; set; }

    public decimal? Shipping { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? Total { get; set; }

    public DateTime? CreateDate { get; set; }

    public string? TransId { get; set; }

    public string? Status { get; set; }

    [StringLength(40)]
    public string PaymentMethod { get; set; } = "CashOnDelivery";

    [StringLength(40)]
    public string PaymentStatus { get; set; } = "PendingCollection";

    [StringLength(100)]
    public string? Carrier { get; set; }

    [StringLength(100)]
    public string? TrackingNumber { get; set; }

    public DateTime? ShippedAtUtc { get; set; }
    public int? ShippedByUserId { get; set; }
    [StringLength(120)]
    public string? ShippedByName { get; set; }

    public virtual ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    public virtual ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public virtual ICollection<OrderPaymentEvent> PaymentEvents { get; set; } = new List<OrderPaymentEvent>();
}
