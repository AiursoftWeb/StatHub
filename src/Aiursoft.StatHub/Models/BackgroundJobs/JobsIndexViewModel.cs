using Aiursoft.UiStack.Layout;

namespace Aiursoft.StatHub.Models.BackgroundJobs;

public class JobsIndexViewModel : UiStackLayoutViewModel
{
    public IEnumerable<JobInfo> AllRecentJobs { get; init; } = [];
}
