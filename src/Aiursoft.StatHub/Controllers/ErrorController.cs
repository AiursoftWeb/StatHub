using System.Diagnostics;
using Aiursoft.StatHub.Models.ErrorViewModels;
using Aiursoft.StatHub.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.StatHub.Controllers;

/// <summary>
/// This controller is used to show error pages.
/// </summary>
public class ErrorController : Controller
{
    [Route("Error/Code{code:int}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Code(int code, [FromQuery] string? returnUrl = null)
    {
        var model = new ErrorViewModel(code)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            ReturnUrl = returnUrl
        };

        return this.StackView(model, viewName: "Error");
    }
}
