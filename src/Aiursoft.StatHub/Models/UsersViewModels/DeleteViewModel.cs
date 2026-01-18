using Aiursoft.StatHub.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.StatHub.Models.UsersViewModels;

public class DeleteViewModel : UiStackLayoutViewModel
{
    public DeleteViewModel()
    {
        PageTitle = "Delete User";
    }

    public required User User { get; set; }
}
