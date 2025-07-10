using EventManagement.Models;
using EventManagement.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Thêm DbContext với cấu hình chuỗi kết nối
builder.Services.AddDbContext<EventManagementDb1Context>();

// Thêm dịch vụ Session
builder.Services.AddSession();

builder.Services.AddSingleton<IVnPayService, VnPayService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();
app.UseSession();


app.UseAuthorization();
// ⚠️ PHẢI gọi session middleware trước khi dùng session
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
