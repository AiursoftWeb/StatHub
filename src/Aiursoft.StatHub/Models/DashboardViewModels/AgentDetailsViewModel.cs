using Aiursoft.StatHub.SDK.Models;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.StatHub.Models.DashboardViewModels;

public class AgentDetailsViewModel : UiStackLayoutViewModel
{
    public AgentDetailsViewModel()
    {
        PageTitle = "Server Details";
    }

    public required Agent Agent { get; init; }
}
