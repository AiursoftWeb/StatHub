using Aiursoft.StatHub.Server.Data;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Server.Controllers;

public class HomeController(InMemoryDatabase database) : Controller
{
    public IActionResult Index([FromQuery]bool last30Seconds = false)
    {
        var clients = database.GetClients();
        ViewBag.Last30Seconds = last30Seconds;
        return View(clients);
    }
    
    public IActionResult Details([FromRoute]string id)
    {
        var client = database.GetOrAddClient(id);
        return View(client);
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
DEBIAN_FRONTEND=noninteractive sudo apt install dotnet8 -y
DEBIAN_FRONTEND=noninteractive sudo apt install pcp -y
sudo touch /etc/motd
sudo rm /root/.local/share/StatHubClient/config.conf
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

        [HttpGet("install-docker.sh")]
    public IActionResult GetInstallScriptForDocker()
    {
        // return text/plain
        var installScript = @$"
curl -fsSL https://get.docker.com -o get-docker.sh
sh get-docker.sh

docker run -d \
    --pid host \
    --net host \
    -v /etc/lsb-release:/etc/lsb-release:ro \
    -v /etc/os-release:/etc/os-release:ro \
    -v /etc/motd:/etc/motd:ro \
    -v /etc/hostname:/etc/hostname:ro \
    --privileged \
    -e SERVER_ENDPOINT=""{Request.Scheme}://{Request.Host}"" \
    hub.aiursoft.cn/aiursoft/stathub-client
";
        return Content(installScript, "text/plain");
    }
}