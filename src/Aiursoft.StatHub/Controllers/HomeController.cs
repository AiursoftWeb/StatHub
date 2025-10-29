using Aiursoft.StatHub.Models.HomeViewModels;
using Aiursoft.StatHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }
}
