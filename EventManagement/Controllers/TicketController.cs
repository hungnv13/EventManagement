using EventManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

public class TicketController : Controller
{
    private readonly EventManagementDb1Context _ctx;

    public TicketController(EventManagementDb1Context ctx)
    {
        _ctx = ctx;
    }

    public IActionResult MyTickets()
    {
        int userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");
        var tickets = _ctx.Tickets.Include(t => t.Event)
                                  .Where(t => t.UserId == userId)
                                  .OrderByDescending(t => t.PurchaseDate)
                                  .ToList();
        return View(tickets);
    }
}
