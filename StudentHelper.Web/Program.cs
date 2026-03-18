using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Налаштування Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// 2. Додавання сервісів MVC
builder.Services.AddControllersWithViews();

// 3. Налаштування контексту бази даних (PostgreSQL)
builder.Services.AddDbContext<StudentHelperDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 4. Налаштування ASP.NET Core Identity
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<StudentHelperDbContext>()
.AddDefaultTokenProviders();

// 5. Налаштування Cookie для Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "StudentHelper.Auth";
});

// 6. Реєстрація сервісів
builder.Services.AddScoped<IAuthService, AuthService>();

// 7. Email Sender (заглушка для розробки - замініть на реальний сервіс)
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

var app = builder.Build();

// --- АВТОМАТИЧНА МІГРАЦІЯ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StudentHelperDbContext>();
        context.Database.Migrate();
        Console.WriteLine(">>> БАЗА ДАНИХ УСПІШНО ОНОВЛЕНА!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> ПОМИЛКА МІГРАЦІЇ: {ex.Message}");
    }
}
// --------------------------------

// 8. Логування запитів Serilog
app.UseSerilogRequestLogging();

// 9. Конфігурація конвеєра HTTP (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ========== EmailSender Implementation ==========
public interface IEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}

public class ConsoleEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"\n[EMAIL]\nЕ-mail: {email}\nТема: {subject}\nПовідомлення: {htmlMessage}\n");
        return Task.CompletedTask;
    }
}