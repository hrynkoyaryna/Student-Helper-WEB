using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;

        public AdminController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> FixStudentRoles()
        {
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                var isUser = await _userManager.IsInRoleAsync(user, "User");

                if (!isAdmin && !isUser)
                {
                    await _userManager.AddToRoleAsync(user, "User");
                }
            }

            TempData["SuccessMessage"] = "Ролі User успішно додані всім студентам.";
            return RedirectToAction("Index", "Students");
        }
    }
}