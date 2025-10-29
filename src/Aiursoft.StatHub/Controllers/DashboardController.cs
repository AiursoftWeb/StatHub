using Aiursoft.StatHub.Models.DashboardViewModels;
using Aiursoft.StatHub.Services;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

public class DashboardController : Controller
{
    [RenderInNavBar(
        NavGroupName = "Features",
        NavGroupOrder = 1,
        CascadedLinksGroupName = "Home",
        CascadedLinksIcon = "home",
        CascadedLinksOrder = 1,
        LinkText = "Index",
        LinkOrder = 1)]
    public IActionResult Index()
    {
        return this.StackView(new IndexViewModel());
    }
}
