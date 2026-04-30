using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Web.Models.UserRequests;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class UserRequestsController : Controller
{
    private readonly StudentHelperDbContext context;
    private readonly UserManager<User> userManager;

    public UserRequestsController(
        StudentHelperDbContext context,
        UserManager<User> userManager)
    {
        this.context = context;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (this.User.IsInRole("Admin"))
        {
            return this.RedirectToAction(nameof(this.AdminIndex));
        }

        var user = await this.userManager.GetUserAsync(this.User);

        if (user == null)
        {
            return this.Unauthorized();
        }

        var requests = await this.context.UserRequests
            .Where(request => request.UserId == user.Id)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();

        return this.View(requests);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return this.View(new CreateUserRequestViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserRequestViewModel model)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var user = await this.userManager.GetUserAsync(this.User);

        if (user == null)
        {
            return this.Unauthorized();
        }

        var request = new UserRequest
        {
            UserId = user.Id,
            Category = model.Category,
            RequestType = model.RequestType,
            Subject = model.Subject,
            Description = model.Description,
            Status = "Нове",
            CreatedAt = DateTime.UtcNow,
        };

        this.context.UserRequests.Add(request);
        await this.context.SaveChangesAsync();

        return this.RedirectToAction(nameof(this.Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminIndex()
    {
        var requests = await this.context.UserRequests
            .Include(request => request.User)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync();

        return this.View(requests);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> EditStatus(int id)
    {
        var request = await this.context.UserRequests.FindAsync(id);

        if (request == null)
        {
            return this.NotFound();
        }

        var model = new UpdateUserRequestStatusViewModel
        {
            Id = request.Id,
            Status = request.Status,
            AdminResponse = request.AdminResponse,
        };

        return this.View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditStatus(UpdateUserRequestStatusViewModel model)
    {
        if (!this.ModelState.IsValid)
        {
            return this.View(model);
        }

        var request = await this.context.UserRequests.FindAsync(model.Id);

        if (request == null)
        {
            return this.NotFound();
        }

        request.Status = model.Status;
        request.AdminResponse = model.AdminResponse;
        request.UpdatedAt = DateTime.UtcNow;

        await this.context.SaveChangesAsync();

        return this.RedirectToAction(nameof(this.AdminIndex));
    }
}