using Aiursoft.StatHub.SDK.Models;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.StatHub.Models.DashboardViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public IndexViewModel()
    {
        PageTitle = "Dashboard";
    }

    public required Agent[] Agents { get; init; }
}
