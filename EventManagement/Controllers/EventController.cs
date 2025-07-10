using EventManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Controllers
{
    public class EventController : Controller
    {
        private readonly EventManagementDb1Context _context;

        public EventController(EventManagementDb1Context context)
        {
            _context = context;
        }

        // GET: /Event/
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(role) || string.IsNullOrEmpty(userIdStr))
                return RedirectToAction("Login", "Account");

            IQueryable<Event> query = _context.Events
                .Include(e => e.User)
                .Where(e => !e.IsDeleted);

            if (role == "User")
            {
                int userId = int.Parse(userIdStr);
                query = query.Where(e => e.UserId == userId);
            }

            var events = query.OrderBy(e => e.StartTime).ToList();
            return View(events);
        }


        // GET: /Event/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null) return NotFound();

            var ev = _context.Events
                .Include(e => e.User)
                .FirstOrDefault(e => e.EventId == id && !e.IsDeleted);

            if (ev == null) return NotFound();

            return View(ev);
        }

        // GET: /Event/Create
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role != "Admin" && role != "User") return Unauthorized();

            return View();
        }

        // POST: /Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Event ev, IFormFile imageFile)
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if ((role != "Admin" && role != "User") || string.IsNullOrEmpty(userIdStr))
                return Unauthorized();

            ev.UserId = int.Parse(userIdStr);
            ev.IsDeleted = false;

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Path.GetFileName(imageFile.FileName);
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ImageUrl");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                ev.ImageUrl = "/ImageUrl/" + fileName;
            }

            if (ModelState.IsValid)
            {
                _context.Events.Add(ev);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(ev);
        }

        // GET: /Event/Edit/5
        public IActionResult Edit(int? id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if ((role != "Admin" && role != "User") || string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            if (id == null) return NotFound();

            var ev = _context.Events.FirstOrDefault(e => e.EventId == id && !e.IsDeleted);
            if (ev == null) return NotFound();

            if (ev.UserId != int.Parse(userIdStr)) return Unauthorized();

            return View(ev);
        }

        // POST: /Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, [Bind("EventId,Title,Description,Location,StartTime,EndTime,ImageUrl")] Event ev)
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if ((role != "Admin" && role != "User") || string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            if (id != ev.EventId) return NotFound();

            var existingEvent = _context.Events.AsNoTracking().FirstOrDefault(e => e.EventId == id && !e.IsDeleted);
            if (existingEvent == null) return NotFound();

            if (existingEvent.UserId != int.Parse(userIdStr)) return Unauthorized();

            ev.UserId = existingEvent.UserId;
            ev.IsDeleted = existingEvent.IsDeleted;

            if (ModelState.IsValid)
            {
                _context.Update(ev);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }

            return View(ev);
        }

        // GET: /Event/Delete/5
        public IActionResult Delete(int? id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if ((role != "Admin" && role != "User") || string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            if (id == null) return NotFound();

            var ev = _context.Events.FirstOrDefault(e => e.EventId == id && !e.IsDeleted);
            if (ev == null) return NotFound();

            // Chỉ từ chối nếu là User và không phải chủ sự kiện
            if (role == "User" && ev.UserId != int.Parse(userIdStr)) return Unauthorized();

            return View(ev);
        }


        // POST: /Event/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var role = HttpContext.Session.GetString("Role");
            var userIdStr = HttpContext.Session.GetString("UserId");

            if ((role != "Admin" && role != "User") || string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var ev = _context.Events.FirstOrDefault(e => e.EventId == id && !e.IsDeleted);
            if (ev == null) return NotFound();

            // Chỉ từ chối nếu là User và không phải chủ sự kiện
            if (role == "User" && ev.UserId != int.Parse(userIdStr)) return Unauthorized();

            // Soft delete
            ev.IsDeleted = true;
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
