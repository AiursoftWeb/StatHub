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

    public IActionResult AddClient()
    {
        return View();
    }

    [HttpGet("install.sh")]
    public IActionResult GetInstallScript()
    {
        // return text/plain
        var installScript = @$"
DEBIAN_FRONTEND=noninteractive sudo apt install dotnet7 -y
DEBIAN_FRONTEND=noninteractive sudo apt install pcp -y
sudo touch /etc/motd
sudo dotnet tool install Aiursoft.StatHub.Client --tool-path /opt/stathub-client || sudo dotnet tool update Aiursoft.StatHub.Client --tool-path /opt/stathub-client

echo ""[Unit]
Description=Stathub Client
After=network.target
Wants=network.target

[Service]
Type=simple
User=root
ExecStart=/opt/stathub-client/stathub-client -s ""{Request.Scheme}://{Request.Host}""
WorkingDirectory=/opt/stathub-client
Restart=always
RestartSec=10
KillSignal=SIGINT

[Install]
WantedBy=multi-user.target"" | sudo tee /etc/systemd/system/stathub.service

sudo systemctl daemon-reload
sudo systemctl enable stathub.service
sudo systemctl stop stathub.service > /dev/null 2>&1
sudo systemctl start stathub.service";
        return Content(installScript, "text/plain");
    }
}