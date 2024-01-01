using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

public class HomeController : Controller
{
    private readonly InMemoryDatabase _database;

    public HomeController(InMemoryDatabase database)
    {
        _database = database;
    }
    
    public IActionResult Index()
    {
        var clients = _database.GetClients();
        return View(clients);
    }
}