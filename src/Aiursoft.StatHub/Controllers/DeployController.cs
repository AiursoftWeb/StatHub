using Aiursoft.StatHub.Models.DeployViewModels;
using Aiursoft.StatHub.Services;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

[LimitPerMin]
public class DeployController : Controller
{
    [AllowAnonymous]
    [RenderInNavBar(
        NavGroupName = "Resources",
        NavGroupOrder = 2,
        CascadedLinksGroupName = "Deploy",
        CascadedLinksIcon = "upload-cloud",
        CascadedLinksOrder = 1,
        LinkText = "Self host a new server",
        LinkOrder = 1)]
    public IActionResult SelfHost()
    {
        return this.StackView(new SelfHostViewModel());
    }
}
