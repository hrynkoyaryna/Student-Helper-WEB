using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using StudentHelper.Web.Models;

namespace StudentHelper.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Calendar");
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        if (HttpContext.Items.TryGetValue("ErrorViewModel", out var model) && model is ErrorViewModel errorModel)
        {
            return View(errorModel);
        }

        return View(new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = "Помилка сервера",
            Message = "При обробці запиту сталась помилка. Спробуйте ще раз.",
            StatusCode = 500
        });
    }
}
