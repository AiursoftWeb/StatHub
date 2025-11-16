using Aiursoft.StatHub.Authorization;
using Aiursoft.StatHub.Data;
using Aiursoft.StatHub.Models.DashboardViewModels;
using Aiursoft.StatHub.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

[LimitPerMin]
public class DashboardController(InMemoryDatabase database) : Controller
{
    [Authorize(Policy = AppPermissionNames.CanViewDashboard)]
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Dashboard",
        CascadedLinksIcon = "align-left",
        CascadedLinksOrder = 1,
        LinkText = "Agents",
        LinkOrder = 1)]
    public IActionResult Index([FromQuery]bool last30Seconds = false)
    {
        var clients = database.GetAgents();
        ViewBag.Last30Seconds = last30Seconds;
        return this.StackView(new IndexViewModel
        {
            Agents = clients
        });
    }

    [Authorize(Policy = AppPermissionNames.CanViewDashboard)]
    public IActionResult Details([FromRoute]string id)
    {
        var client = database.GetClient(id);
        if (client == null)
        {
            return NotFound();
        }
        return this.StackView(new AgentDetailsViewModel
        {
            Agent = client
        });
    }

    [Authorize(Policy = AppPermissionNames.CanViewDashboard)]
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Dashboard",
        CascadedLinksIcon = "align-left",
        CascadedLinksOrder = 1,
        LinkText = "Add an agent",
        LinkOrder = 1)]
    public IActionResult AddAgent()
    {
        return this.StackView(new AddAgentViewModel());
    }

    [HttpGet("install.sh")]
    [AllowAnonymous]
    public IActionResult GetInstallScript()
    {
        // return text/plain
        var installScript = @$"

#if [[ ""$(lsb_release -sc)"" =~ ^(devel|jammy|noble)$ ]]; then
#  sudo add-apt-repository ppa:dotnet/backports --yes
#fi

wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh && \
    chmod +x /tmp/dotnet-install.sh && \
    sudo /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet && \
    sudo ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet && \
    rm /tmp/dotnet-install.sh
DEBIAN_FRONTEND=noninteractive sudo apt update
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
}
