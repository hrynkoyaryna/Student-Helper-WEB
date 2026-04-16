using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Domain.Entities;
using StudentHelper.Web.Models.Students;

namespace StudentHelper.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentsController : Controller
    {
        private readonly UserManager<User> _userManager;

        public StudentsController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
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
            return View(new CreateStudentViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(model.UserName) || model.UserName.Contains(' '))
            {
                ModelState.AddModelError(nameof(model.UserName), "Логін має бути одним словом, без пробілів.");
                return View(model);
            }

            var existingUserByName = await _userManager.FindByNameAsync(model.UserName);
            if (existingUserByName != null)
            {
                ModelState.AddModelError(nameof(model.UserName), "Користувач із таким логіном уже існує.");
                return View(model);
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingUserByEmail != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Користувач із такою електронною поштою вже існує.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.UserName,
                Email = model.Email,
                EmailConfirmed = true,
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "User");

            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult);
                await _userManager.DeleteAsync(user);
                return View(model);
            }

            TempData["SuccessMessage"] = "Профіль студента успішно створено.";
            return RedirectToAction(nameof(Index));
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
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditStudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            if (string.IsNullOrWhiteSpace(model.UserName) || model.UserName.Contains(' '))
            {
                ModelState.AddModelError(nameof(model.UserName), "Логін має бути одним словом, без пробілів.");
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
                ModelState.AddModelError(nameof(model.UserName), "Користувач із таким логіном уже існує.");
                return View(model);
            }

            var userWithSameEmail = await _userManager.FindByEmailAsync(model.Email);
            if (userWithSameEmail != null && userWithSameEmail.Id != model.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "Користувач із такою електронною поштою вже існує.");
                return View(model);
            }

            user.UserName = model.UserName;
            user.Email = model.Email;

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                AddIdentityErrors(result);
                return View(model);
            }

            TempData["SuccessMessage"] = "Профіль студента успішно оновлено.";
            return RedirectToAction(nameof(Index));
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
                TempData["ErrorMessage"] = "Не вдалося видалити профіль студента.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Профіль студента успішно видалено.";
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

            TempData["SuccessMessage"] = "Пароль студента успішно змінено.";
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