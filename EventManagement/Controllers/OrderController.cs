using EventManagement.Helpers;
using EventManagement.Models;
using EventManagement.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;

public class OrderController : Controller
{
    private readonly EventManagementDb1Context _ctx;
    private readonly IVnPayService _vnPayService;

    public OrderController(EventManagementDb1Context ctx, IVnPayService vnPayService)
    {
        _ctx = ctx;
        _vnPayService = vnPayService;
    }


    [HttpGet]
    public IActionResult Order(int eventId)
    {
        var ev = _ctx.Events.Find(eventId);
        if (ev == null) return NotFound();

        ViewBag.Event = ev;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Payment(int eventId, int quantity)
    {
        var ev = _ctx.Events.Find(eventId);
        if (ev == null) return NotFound();

        if (!HttpContext.Session.TryGetValue("UserId", out byte[] userIdBytes))
            return RedirectToAction("Login", "Account");

        int userId = int.Parse(System.Text.Encoding.UTF8.GetString(userIdBytes));
        var user = _ctx.UserAccounts.Find(userId);
        if (user == null) return Unauthorized();

        var model = new VnPaymentRequestModel
        {
            Amount = (double)(ev.Price * quantity),
            CreatedDate = DateTime.Now,
            OrderId = $"EVT{eventId}_USR{userId}_QTY{quantity}_{DateTime.Now.Ticks}"
        };

        var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, model);
        TempData["EventId"] = eventId;
        TempData["Quantity"] = quantity;
        TempData["UserId"] = userId;

        return Redirect(paymentUrl);
    }



    [HttpGet]
    public IActionResult PaymentCallBack()
    {
        var response = _vnPayService.PaymentExecute(Request.Query);
        if (!response.Success)
        {
            ViewBag.Error = "❌ Thanh toán thất bại hoặc bị từ chối.";
            return View("Error");
        }

        // Lấy thông tin tạm từ TempData
        int eventId = Convert.ToInt32(TempData["EventId"]);
        int quantity = Convert.ToInt32(TempData["Quantity"]);
        int userId = Convert.ToInt32(TempData["UserId"]);

        var ev = _ctx.Events.Find(eventId);
        var user = _ctx.UserAccounts.Find(userId);
        if (ev == null || user == null) return NotFound();

        var order = new Order
        {
            UserId = userId,
            Amount = ev.Price * quantity,
            Status = "Success",
            CreatedAt = DateTime.Now,
            PaidAt = DateTime.Now,
            Note = $"Thanh toán VNPAY: {response.OrderDescription}"
        };
        _ctx.Orders.Add(order);
        _ctx.SaveChanges();

        var ticket = new Ticket
        {
            EventId = eventId,
            UserId = userId,
            Quantity = quantity,
            PurchaseDate = DateTime.Now,
            IsPaid = true
        };
        _ctx.Tickets.Add(ticket);
        _ctx.SaveChanges();

        order.TicketId = ticket.TicketId;
        _ctx.SaveChanges();

        // Gửi email (giữ nguyên phần gửi QR code như trước)
        try
        {
            string qrContent = $"Sự kiện: {ev.Title}\nNgười mua: {user.Username}\nSố lượng: {quantity}\nNgày mua: {ticket.PurchaseDate:dd/MM/yyyy HH:mm}";
            string fileName = $"ticket_{ticket.TicketId}_{DateTime.Now.Ticks}.png";
            string tempPath = Path.Combine(Path.GetTempPath(), fileName);

            using (var qrGen = new QRCodeGenerator())
            {
                var qrData = qrGen.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrData);
                var qrBytes = qrCode.GetGraphic(20);
                System.IO.File.WriteAllBytes(tempPath, qrBytes);
            }

            using (var mail = new MailMessage("nguyenviethungdz2003@gmail.com", user.Email))
            {
                mail.Subject = "🎫 Vé sự kiện của bạn";
                mail.IsBodyHtml = true;
                mail.Body = $@"
                <h3>✅ Bạn đã đặt vé thành công!</h3>
                <p><b>Sự kiện:</b> {ev.Title}</p>
                <p><b>Người mua:</b> {user.Username}</p>
                <p><b>Số lượng vé:</b> {quantity}</p>
                <p><b>Ngày mua:</b> {ticket.PurchaseDate:dd/MM/yyyy HH:mm}</p>
                <hr />
                <p>🎟️ Mã QR vé của bạn được đính kèm trong email này.</p>";

                var attachment = new Attachment(tempPath);
                mail.Attachments.Add(attachment);

                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new NetworkCredential("nguyenviethungdz2003@gmail.com", "qfvp squc ontx qbxk");
                    smtp.EnableSsl = true;
                    smtp.Send(mail);
                }

                attachment.Dispose();
                if (System.IO.File.Exists(tempPath)) System.IO.File.Delete(tempPath);
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "❌ Gửi email thất bại: " + ex.Message;
            return View("Error");
        }

        return View("PaymentSuccess");
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreatePaymentUrl(int eventId, int quantity)
    {
        var ev = _ctx.Events.Find(eventId);
        if (ev == null) return NotFound();

        if (!HttpContext.Session.TryGetValue("UserId", out byte[] userIdBytes))
            return RedirectToAction("Login", "Account");

        int userId = int.Parse(System.Text.Encoding.UTF8.GetString(userIdBytes));
        var user = _ctx.UserAccounts.Find(userId);
        if (user == null) return Unauthorized();

        // ✅ Tạo model gửi sang VnPayService
        var model = new VnPaymentRequestModel
        {
            Amount = (double)(ev.Price * quantity),
            CreatedDate = DateTime.Now,
            OrderId = $"EVT{eventId}_USR{userId}_QTY{quantity}_{DateTime.Now.Ticks}"
        };

        // ✅ Gọi service đã được inject (của bạn đã có rồi)
        var paymentUrl = _vnPayService.CreatePaymentUrl(HttpContext, model);

        // ✅ Lưu thông tin cần thiết để xử lý sau
        TempData["EventId"] = eventId;
        TempData["Quantity"] = quantity;
        TempData["UserId"] = userId;

        return Redirect(paymentUrl);
    }
}
