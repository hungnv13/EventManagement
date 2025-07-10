using EventManagement.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class AccountController : Controller
{
    private readonly EventManagementDb1Context _context;

    public AccountController(EventManagementDb1Context context)
    {
        _context = context;
    }

    // Login
    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
            return View();
        }

        var user = _context.UserAccounts
            .FirstOrDefault(u => u.Username == username && u.Password == password);

        if (user != null)
        {
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Role", user.Role);

            return user.Role == "Admin"
                ? RedirectToAction("AdminDashboard", "Home")
                : RedirectToAction("UserDashboard", "Home");
        }

        ViewBag.Error = "Sai tài khoản hoặc mật khẩu!";
        return View();
    }

    // Register
    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(UserAccount model)
    {
        if (string.IsNullOrEmpty(model.Username) || string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
            return View();
        }

        var exists = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

        if (exists != null)
        {
            ViewBag.Error = "Tên đăng nhập hoặc Email đã tồn tại.";
            return View();
        }

        model.Role = "User";
        _context.UserAccounts.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Login");
    }

    // Forgot Password
    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string username, string email)
    {
        var user = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Username == username && u.Email == email);

        if (user == null)
        {
            ViewBag.Error = "Không tìm thấy tài khoản phù hợp.";
            return View();
        }

        string newPassword = "Ghsaus";
        user.Password = newPassword;
        await _context.SaveChangesAsync();

        try
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("nguyenviethungdz2003@gmail.com", "qfvp squc ontx qbxk"),
                EnableSsl = true,
            };

            var mail = new MailMessage("nguyenviethungdz2003@gmail.com", user.Email)
            {
                Subject = "Khôi phục mật khẩu - EventManagement",
                Body = $"Xin chào {user.Username},\n\nMật khẩu mới: {newPassword}\nVui lòng đăng nhập và đổi mật khẩu.\n\nTrân trọng."
            };

            await smtpClient.SendMailAsync(mail);
            ViewBag.Message = "Mật khẩu mới đã được gửi đến email.";
        }
        catch
        {
            ViewBag.Error = "Gửi email thất bại. Vui lòng thử lại sau.";
        }

        return View();
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }

    // ==============================
    // Edit tài khoản (người dùng hoặc admin)
    // ==============================
    [HttpGet]
    public async Task<IActionResult> Edit(int? id)
    {
        string role = HttpContext.Session.GetString("Role");
        int userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

        UserAccount user;

        if (role == "Admin" && id.HasValue)
        {
            user = await _context.UserAccounts.FindAsync(id.Value);
            ViewBag.IsAdmin = true;
        }
        else
        {
            user = await _context.UserAccounts.FindAsync(userId);
            ViewBag.IsAdmin = false;
        }

        return user == null ? NotFound() : View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserAccount updatedUser)
    {
        string role = HttpContext.Session.GetString("Role");
        bool isAdmin = role == "Admin";
        ViewBag.IsAdmin = isAdmin;

        var user = await _context.UserAccounts.FindAsync(updatedUser.UserId);
        if (user == null) return NotFound();

        user.Username = updatedUser.Username;
        user.Email = updatedUser.Email;

        if (isAdmin)
        {
            user.Role = updatedUser.Role;
            user.Password = updatedUser.Password;
        }

        await _context.SaveChangesAsync();

        if (HttpContext.Session.GetString("UserId") == updatedUser.UserId.ToString())
        {
            HttpContext.Session.SetString("Username", user.Username);
        }

        ViewBag.Message = "Cập nhật thành công!";
        return View(user);
    }

    // ==============================
    // Đổi mật khẩu
    // ==============================
    [HttpGet]
    public async Task<IActionResult> ChangePassword()
    {
        var id = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Login");

        var user = await _context.UserAccounts.FindAsync(int.Parse(id));
        return user == null ? NotFound() : View(user);
    }

    [HttpPost]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        var id = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(id)) return RedirectToAction("Login");

        var user = await _context.UserAccounts.FindAsync(int.Parse(id));
        if (user == null) return NotFound();

        if (user.Password != currentPassword)
        {
            ViewBag.Error = "Mật khẩu hiện tại không đúng.";
            return View(user);
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View(user);
        }

        user.Password = newPassword;
        await _context.SaveChangesAsync();

        ViewBag.Message = "Đổi mật khẩu thành công!";
        return View(user);
    }

    // ==============================
    // Quản lý tài khoản (Admin)
    // ==============================
    public async Task<IActionResult> Index()
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin") return RedirectToAction("Login");

        return View(await _context.UserAccounts.ToListAsync());
    }

    public async Task<IActionResult> Delete(int id)
    {
        var role = HttpContext.Session.GetString("Role");
        if (role != "Admin") return RedirectToAction("Login");

        var user = await _context.UserAccounts.FindAsync(id);
        if (user == null) return NotFound();

        _context.UserAccounts.Remove(user);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
