using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHelper.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : BaseController
{
    public IActionResult Index()
    {
        return View();
    }
}