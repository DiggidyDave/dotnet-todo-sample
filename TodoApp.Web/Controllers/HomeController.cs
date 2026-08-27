using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        // Redirect authenticated users to their task list
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Task");
        }

        return View();
    }

    public IActionResult Error()
    {
        return View();
    }
}
