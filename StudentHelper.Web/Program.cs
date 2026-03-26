using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Services;
using StudentHelper.Infrastructure.Services;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using Serilog;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System.Net.Mail;
using StudentHelper.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<StudentHelperDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<IUserService, UserService>();

// 7. Email Sender - bind SMTP settings and register SMTP implementation
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));

// Choose email sender implementation based on configuration
var smtpOptions = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>();
if (smtpOptions != null && !string.IsNullOrEmpty(smtpOptions.Host) && !string.IsNullOrEmpty(smtpOptions.UserName) && !string.IsNullOrEmpty(smtpOptions.Password))
{
    // Use real SMTP sender when SMTP is configured
    builder.Services.AddSingleton<StudentHelper.Application.Interfaces.IEmailSender, SmtpEmailSender>();
}
else
{
    // Fallback to console sender (useful for local development)
    builder.Services.AddSingleton<StudentHelper.Application.Interfaces.IEmailSender, ConsoleEmailSender>();
}

builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IPersonalEventRepository, PersonalEventRepository>();
builder.Services.AddScoped<StudentHelper.Application.Interfaces.INotesRepository, NotesRepository>();
builder.Services.AddScoped<INotesService, NotesService>();
var app = builder.Build();

// ========== GLOBAL EXCEPTION HANDLER ==========
app.UseMiddleware<StudentHelper.Web.Middleware.GlobalExceptionHandlerMiddleware>();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// ========== EmailSender Implementation using MailKit ==========
public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 25;
    public bool UseSsl { get; set; } = true;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? FromName { get; set; }
    public string? FromEmail { get; set; }
}

public class SmtpEmailSender : StudentHelper.Application.Interfaces.IEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();

        // Determine From address with validation
        var fromEmail = _options.FromEmail ?? _options.UserName ?? "noreply@example.com";
        var fromName = _options.FromName ?? "Student Helper";

        try
        {
            // Validate using System.Net.Mail.MailAddress
            var _ = new System.Net.Mail.MailAddress(fromEmail);
            message.From.Add(new MimeKit.MailboxAddress(fromName, fromEmail));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Invalid From email '{FromEmail}', falling back to noreply@example.com", fromEmail);
            message.From.Add(new MimeKit.MailboxAddress("Student Helper", "noreply@example.com"));
        }

        // Validate recipient
        try
        {
            var _ = new System.Net.Mail.MailAddress(email);
            message.To.Add(MimeKit.MailboxAddress.Parse(email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid recipient email: {Email}", email);
            throw new ArgumentException("Invalid recipient email address", nameof(email));
        }

        message.Subject = subject;

        var body = new BodyBuilder { HtmlBody = htmlMessage };
        message.Body = body.ToMessageBody();

        try
        {
            using var client = new MailKit.Net.Smtp.SmtpClient();
            // Office365 requires STARTTLS on port 587
            var secureSocket = MailKit.Security.SecureSocketOptions.StartTls;
            if (_options.Port == 465)
            {
                secureSocket = MailKit.Security.SecureSocketOptions.SslOnConnect;
            }

            await client.ConnectAsync(_options.Host ?? "localhost", _options.Port, secureSocket);

            if (!string.IsNullOrEmpty(_options.UserName))
            {
                await client.AuthenticateAsync(_options.UserName, _options.Password);
            }

            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email} with subject {Subject}", email, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            throw;
        }
    }
}

// Simple console email sender for development
public class ConsoleEmailSender : StudentHelper.Application.Interfaces.IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogInformation("[EMAIL] To: {Email}; Subject: {Subject}; Body: {Body}", email, subject, htmlMessage);
        Console.WriteLine($"\n[EMAIL]\nTo: {email}\nSubject: {subject}\n{htmlMessage}\n");
        return Task.CompletedTask;
    }
}
