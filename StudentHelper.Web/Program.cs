using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using Serilog;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Repositories;
using StudentHelper.Infrastructure.Services;
using StudentHelper.Web.Middleware;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();

builder.Services.Configure<ApplicationSettings>(
    builder.Configuration.GetSection("ApplicationSettings"));

builder.Services.AddDbContext<StudentHelperDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IGroupsService, GroupsService>();

var appSettings = builder.Configuration.GetSection("ApplicationSettings").Get<ApplicationSettings>();

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    if (appSettings?.PasswordSettings != null)
    {
        options.Password.RequireDigit = appSettings.PasswordSettings.RequireDigit;
        options.Password.RequiredLength = appSettings.PasswordSettings.RequiredLength;
        options.Password.RequireNonAlphanumeric = appSettings.PasswordSettings.RequireNonAlphanumeric;
        options.Password.RequireUppercase = appSettings.PasswordSettings.RequireUppercase;
        options.Password.RequireLowercase = appSettings.PasswordSettings.RequireLowercase;
    }
    else
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
    }

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<StudentHelperDbContext>()
.AddDefaultTokenProviders();

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

builder.Services.AddScoped<IPersonalEventRepository, PersonalEventRepository>();
builder.Services.AddScoped<IExamsRepository, ExamsRepository>();
builder.Services.AddScoped<INotesRepository, NotesRepository>();
builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();

// Register schedule repository and service
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleService, StudentHelper.Infrastructure.Services.ScheduleService>();

builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<INotesService, NotesService>();
builder.Services.AddScoped<IExamsService, ExamsService>();

// Add memory cache and cacheable lookup service
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheableLookupService, StudentHelper.Web.Services.CacheableLookupServiceWeb>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
var smtpOptions = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>();

if (smtpOptions != null && !string.IsNullOrEmpty(smtpOptions.Host))
{
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();
}

var app = builder.Build();

var currentEnv = app.Environment.EnvironmentName;
var itemsPerPage = app.Configuration["ApplicationSettings:ItemsPerPage"];
var smtpHost = app.Configuration["Smtp:Host"];

Console.WriteLine("\n" + new string('=', 50));
Console.WriteLine($">>> ����²��� ���Բ����ֲ�");
Console.WriteLine($">>> ������� ����������: {currentEnv}");
Console.WriteLine($">>> ItemsPerPage (� �����): {itemsPerPage}");
Console.WriteLine($">>> SMTP Host: {(string.IsNullOrEmpty(smtpHost) ? "�������� (Console Mode)" : smtpHost)}");
Console.WriteLine(new string('=', 50) + "\n");

// Додаємо middleware для обробки глобальних винятків
app.UseMiddleware<StudentHelper.Web.Middleware.GlobalExceptionHandlerMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<StudentHelperDbContext>();
        context.Database.Migrate();

        await DbSeeder.SeedAdminAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>> Error: {ex.Message}");
    }
}

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// Додаємо middleware для логування запитів (повинен бути перед іншими)
app.UseRequestLogging();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 25;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromEmail { get; set; }
}

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions options;

    public SmtpEmailSender(IOptions<SmtpOptions> options)
    {
        this.options = options.Value;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Student Helper", this.options.FromEmail ?? "noreply@test.com"));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();

        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(this.options.Host, this.options.Port, MailKit.Security.SecureSocketOptions.StartTls);

        if (!string.IsNullOrEmpty(this.options.UserName))
        {
            await client.AuthenticateAsync(this.options.UserName, this.options.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

public class ConsoleEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"[Email to {email}]: {subject}");
        return Task.CompletedTask;
    }
}