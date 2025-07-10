using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

public class HomeController : Controller
{
    private readonly EventManagementDb1Context _context;

    public HomeController(EventManagementDb1Context context)
    {
        _context = context;
    }

    public IActionResult AdminDashboard()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin")
            return RedirectToAction("Login", "Account");

        ViewBag.Username = HttpContext.Session.GetString("Username");
        return View();
    }

    public IActionResult StaffDashboard()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Staff")
            return RedirectToAction("Login", "Account");

        ViewBag.Username = HttpContext.Session.GetString("Username");
        return View();
    }

    public IActionResult UserDashboard(string q)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "User")
            return RedirectToAction("Login", "Account");

        ViewBag.Username = HttpContext.Session.GetString("Username");

        var events = _context.Events
            .Include(e => e.User)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(q))
        {
            q = q.ToLower();
            events = events.Where(e =>
                e.Title.ToLower().Contains(q) ||
                e.Description.ToLower().Contains(q) ||
                e.Location.ToLower().Contains(q));
        }

        return View(events.ToList());
    }

    // ✅ Action để xử lý tìm kiếm toàn cục từ navbar
    public IActionResult Search(string q)
    {
        var role = HttpContext.Session.GetString("Role");
        if (string.IsNullOrEmpty(role))
            return RedirectToAction("Login", "Account");

        ViewBag.Username = HttpContext.Session.GetString("Username");
        ViewBag.Query = q;

        var events = _context.Events
            .Include(e => e.User)
            .Where(e => !e.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.ToLower();
            events = events.Where(e =>
                e.Title.ToLower().Contains(q) ||
                e.Description.ToLower().Contains(q) ||
                e.Location.ToLower().Contains(q));
        }

        return View("Search", events.ToList());
    }
}
