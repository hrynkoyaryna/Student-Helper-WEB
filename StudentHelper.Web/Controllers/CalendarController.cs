using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace StudentHelper.Web.Controllers;

[Authorize]
public class CalendarController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
