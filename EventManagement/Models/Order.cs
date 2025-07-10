using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? TicketId { get; set; }

    public int UserId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? Note { get; set; }

    public virtual Ticket Ticket { get; set; } = null!;

    public virtual UserAccount User { get; set; } = null!;
}
