using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Students;

namespace StudentHelper.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly StudentHelper.Infrastructure.Data.StudentHelperDbContext? _dbContext;
        private readonly ILogger<StudentsController> _logger;

        // Make dbContext optional to avoid breaking unit tests that don't provide it
        public StudentsController(UserManager<User> userManager, StudentHelper.Infrastructure.Data.StudentHelperDbContext? dbContext = null, ILogger<StudentsController>? logger = null)
        {
            _userManager = userManager;
            _dbContext = dbContext;
            _logger = logger ?? LoggerFactory.Create(builder => { }).CreateLogger<StudentsController>();
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Loading students list");

            // Include Group navigation so view can access student.Group?.Name
            IQueryable<User> usersQuery = _userManager.Users;
            try
            {
                usersQuery = usersQuery.Include(u => u.Group);
            }
            catch
            {
                // If Include is not supported (e.g., in some test setups), fall back to usersQuery
            }

            var users = await usersQuery.ToListAsync();
            var students = new List<User>();

            foreach (var user in users)
            {
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    students.Add(user);
                }
            }

            return View(students);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.GroupNames = _dbContext != null
                ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                : new List<string>();
            return View(new CreateStudentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                model.UserName = model.UserName.Trim();
                model.Email = model.Email.Trim();
                model.FirstName = model.FirstName.Trim();
                model.LastName = model.LastName.Trim();
                if (!string.IsNullOrWhiteSpace(model.GroupName)) model.GroupName = model.GroupName!.Trim();

                if (string.IsNullOrWhiteSpace(model.UserName) || model.UserName.Contains(' '))
                {
                    ModelState.AddModelError(nameof(model.UserName), "Логін не може містити пробілів, або бути пустим.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                var existingUserByName = await _userManager.FindByNameAsync(model.UserName);
                if (existingUserByName != null)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Користувач з таким логіном вже існує.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    ModelState.AddModelError(nameof(model.Email), "Користувач з такою електронною поштою вже існує.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                int? groupId = null;
                if (!string.IsNullOrWhiteSpace(model.GroupName) && _dbContext != null)
                {
                    groupId = await GetOrCreateGroupAsync(model.GroupName!);
                }

                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    EmailConfirmed = true,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    GroupId = groupId
                };

                // Create user
                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed to create user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
                    AddIdentityErrors(result);
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "User");

                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Failed to add role to user: {Errors}", string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                    AddIdentityErrors(roleResult);
                    await _userManager.DeleteAsync(user);
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                _logger.LogInformation("User {UserName} created with GroupId {GroupId}", user.UserName, user.GroupId);

                TempData["SuccessMessage"] = "Користувача успішно додано.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating user");
                ModelState.AddModelError(string.Empty, "Сталася помилка. Спробуйте пізніше.");

                ViewBag.GroupNames = _dbContext != null
                    ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                    : new List<string>();
                return View(model);
            }
        }

        private async Task<int> GetOrCreateGroupAsync(string groupName)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(groupName)) throw new ArgumentException("Group name is required", nameof(groupName));
            var trimmed = groupName.Trim();
            if (_dbContext == null) throw new InvalidOperationException("DbContext is not available");

            // Use a transaction to avoid race conditions
            using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Name.ToLower() == trimmed.ToLower());
                if (existing != null)
                {
                    await tx.CommitAsync();
                    return existing.Id;
                }

                var group = new Group { Name = trimmed };
                _dbContext.Groups.Add(group);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Created new group {GroupName} with Id {GroupId}", trimmed, group.Id);
                return group.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create or get group {GroupName}", groupName);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            var model = new EditStudentViewModel
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                GroupName = user.Group?.Name
            };

            ViewBag.GroupNames = _dbContext != null
                ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                : new List<string>();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditStudentViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                model.UserName = model.UserName.Trim();
                model.Email = model.Email.Trim();
                model.FirstName = model.FirstName.Trim();
                model.LastName = model.LastName.Trim();
                if (!string.IsNullOrWhiteSpace(model.GroupName)) model.GroupName = model.GroupName!.Trim();

                if (string.IsNullOrWhiteSpace(model.UserName) || model.UserName.Contains(' '))
                {
                    ModelState.AddModelError(nameof(model.UserName), "Логін не може містити пробілів, або бути пустим.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                var user = await _userManager.FindByIdAsync(model.Id.ToString());

                if (user == null)
                {
                    return NotFound();
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return Forbid();
                }

                var userWithSameName = await _userManager.FindByNameAsync(model.UserName);
                if (userWithSameName != null && userWithSameName.Id != model.Id)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Користувач з таким логіном вже існує.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
                if (userWithSameEmail != null && userWithSameEmail.Id != model.Id)
                {
                    ModelState.AddModelError(nameof(model.Email), "Користувач з такою електронною поштою вже існує.");
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                int? groupId = null;
                if (!string.IsNullOrWhiteSpace(model.GroupName) && _dbContext != null)
                {
                    groupId = await GetOrCreateGroupAsync(model.GroupName!);
                }

                user.UserName = model.UserName;
                user.Email = model.Email;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.GroupId = groupId;

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    AddIdentityErrors(result);
                    ViewBag.GroupNames = _dbContext != null
                        ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                        : new List<string>();
                    return View(model);
                }

                _logger.LogInformation("User {UserId} updated. GroupId: {GroupId}", user.Id, user.GroupId);

                TempData["SuccessMessage"] = "Користувача успішно оновлено.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating user");
                ModelState.AddModelError(string.Empty, "Сталася помилка. Спробуйте пізніше.");

                ViewBag.GroupNames = _dbContext != null
                    ? _dbContext.Groups.OrderBy(g => g.Name).Select(g => g.Name).ToList()
                    : new List<string>();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            var result = await _userManager.DeleteAsync(user);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Не вдалося видалити користувача.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Користувача успішно видалено.";
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            var model = new AdminChangeStudentPasswordViewModel
            {
                Id = user.Id,
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AdminChangeStudentPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id.ToString());

            if (user == null)
            {
                return NotFound();
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            TempData["SuccessMessage"] = "Пароль користувача успішно змінено.";
            return RedirectToAction(nameof(Index));
        }

        private void AddIdentityErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}