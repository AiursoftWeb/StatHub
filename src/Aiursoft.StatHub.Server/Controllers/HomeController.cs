using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}