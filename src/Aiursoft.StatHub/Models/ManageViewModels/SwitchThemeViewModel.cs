using System.ComponentModel.DataAnnotations;

namespace Aiursoft.StatHub.Models.ManageViewModels;

public class SwitchThemeViewModel
{
    [Required]
    public required string Theme { get; set; }
}
