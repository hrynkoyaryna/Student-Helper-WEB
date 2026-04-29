using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHelper.Application.Interfaces;
using StudentHelper.Domain.Entities;

namespace StudentHelper.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IAccountService _accountService;

        public AdminController(UserManager<User> userManager, IAccountService accountService)
        {
            _userManager = userManager;
            _accountService = accountService;
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

            TempData["SuccessMessage"] = "��� User ������ ������ ��� ���������.";
            return RedirectToAction("Index", "Students");
        }

        // ========== STUDENT BLOCKING ENDPOINTS ==========
        [HttpPost]
        public async Task<IActionResult> BlockStudent(int id)
        {
            var result = await _accountService.BlockStudentAsync(id);
            
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index", "Students");
        }

        [HttpPost]
        public async Task<IActionResult> UnblockStudent(int id)
        {
            var result = await _accountService.UnblockStudentAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = result.Message;
            }

            return RedirectToAction("Index", "Students");
        }
    }
}