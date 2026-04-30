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
using StudentHelper.Infrastructure.Repositories;
using StudentHelper.Application.Models;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllersWithViews();

builder.Services.Configure<ApplicationSettings>(builder.Configuration.GetSection("ApplicationSettings"));

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

builder.Services.AddScoped<IPersonalEventRepository, PersonalEventRepository>();
builder.Services.AddScoped<IExamsRepository, ExamsRepository>(); 
builder.Services.AddScoped<StudentHelper.Application.Interfaces.INotesRepository, NotesRepository>();
builder.Services.AddScoped<StudentHelper.Application.Interfaces.ITeacherRepository, TeacherRepository>();

builder.Services.AddScoped<StudentHelper.Application.Services.ICalendarService, StudentHelper.Application.Services.CalendarService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<INotesService, NotesService>();
builder.Services.AddScoped<IExamsService, ExamsService>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
var smtpOptions = builder.Configuration.GetSection("Smtp").Get<SmtpOptions>();
if (smtpOptions != null && !string.IsNullOrEmpty(smtpOptions.Host))
{
    builder.Services.AddSingleton<StudentHelper.Application.Interfaces.IEmailSender, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<StudentHelper.Application.Interfaces.IEmailSender, ConsoleEmailSender>();
}

var app = builder.Build();

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

public class SmtpEmailSender : StudentHelper.Application.Interfaces.IEmailSender
{
    private readonly SmtpOptions _options;
    public SmtpEmailSender(IOptions<SmtpOptions> options) => _options = options.Value;
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Student Helper", _options.FromEmail ?? "noreply@test.com"));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlMessage }.ToMessageBody();
        using var client = new MailKit.Net.Smtp.SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, MailKit.Security.SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_options.UserName, _options.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

public class ConsoleEmailSender : StudentHelper.Application.Interfaces.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        Console.WriteLine($"[Email to {email}]: {subject}");
        return Task.CompletedTask;
    }
}