using System;
using System.Collections.Generic;

namespace EventManagement.Models;

public partial class Event
{
    public int EventId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Location { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int? UserId { get; set; }

    public string? ImageUrl { get; set; }

    public decimal Price { get; set; }

    public bool IsDeleted { get; set; }

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual UserAccount? User { get; set; }
}
