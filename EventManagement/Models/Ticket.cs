using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Ticket
{
    public int? TicketId { get; set; }

    public int EventId { get; set; }

    public int UserId { get; set; }

    public int Quantity { get; set; }

    public DateTime PurchaseDate { get; set; }

    public bool IsPaid { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual UserAccount User { get; set; } = null!;
}
