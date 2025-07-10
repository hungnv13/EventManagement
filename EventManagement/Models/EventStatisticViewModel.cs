using System;

namespace EventManagement.ViewModels
{
    public class EventStatisticViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalTickets { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
