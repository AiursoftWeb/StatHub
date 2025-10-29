using Aiursoft.StatHub.Configuration;
using Aiursoft.StatHub.Entities;
using Aiursoft.StatHub.Models.ManageViewModels;
using Aiursoft.StatHub.Services;
using Aiursoft.StatHub.Services.FileStorage;
using Aiursoft.UiStack.Navigation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aiursoft.StatHub.Controllers;

/// <summary>
/// This controller is used to handle user related actions like change password, change avatar.
/// </summary>
[Authorize]
public class ManageController(
    ImageProcessingService image,
    StorageService storageService,
    IStringLocalizer<ManageController> localizer,
    IOptions<AppSettings> appSettings,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    ILogger<ManageController> logger)
    : Controller
{
    //
    // GET: /Manage/Index
    [RenderInNavBar(
        NavGroupName = "Settings",
        NavGroupOrder = 9998,
        CascadedLinksGroupName = "Personal",
        CascadedLinksIcon = "user-circle",
        CascadedLinksOrder = 1,
        LinkText = "Profile Settings",
        LinkOrder = 3)]
    [HttpGet]
    public IActionResult Index(ManageMessageId? message = null)
    {
        ViewData["StatusMessage"] =
            message == ManageMessageId.ChangeProfileSuccess ? localizer["Your profile has been saved."] :
            message == ManageMessageId.ChangeAvatarSuccess ? localizer["Your avatar has been saved."] :
            message == ManageMessageId.ChangePasswordSuccess ? localizer["Your password has been changed."] :
            message == ManageMessageId.Error ? localizer["An error has occurred."]
            : "";

        var model = new IndexViewModel();
        return this.StackView(model);
    }

    //
    // GET: /Manage/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (appSettings.Value.OIDCEnabled)
        {
            return BadRequest("Local password is disabled when OIDC authentication is enabled.");
        }
        return this.StackView(new ChangePasswordViewModel());
    }

    //
    // POST: /Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (appSettings.Value.OIDCEnabled)
        {
            return BadRequest("Local password is disabled when OIDC authentication is enabled.");
        }
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await userManager.ChangePasswordAsync(user, model.OldPassword!, model.NewPassword!);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                logger.LogInformation(3, "User changed their password successfully");
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return this.StackView(model);
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/ChangeProfile
    [HttpGet]
    public async Task<IActionResult> ChangeProfile()
    {
        var user = await GetCurrentUserAsync();
        return this.StackView(new ChangeProfileViewModel
        {
            Name = user!.DisplayName
        });
    }

    //
    // POST: /Manage/ChangeProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeProfile(ChangeProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            user.DisplayName = model.Name;
            await userManager.UpdateAsync(user);
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangeProfileSuccess });
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/ChangeAvatar
    [HttpGet]
    public async Task<IActionResult> ChangeAvatar()
    {
        var user = await GetCurrentUserAsync();
        return this.StackView(new ChangeAvatarViewModel
        {
            AvatarUrl = user!.AvatarRelativePath
        });
    }

    //
    // POST: /Manage/ChangeAvatar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAvatar(ChangeAvatarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        // Make sure the file is actually a photo.
        var absolutePath = storageService.GetFilePhysicalPath(model.AvatarUrl);
        if (!await image.IsValidImageAsync(absolutePath))
        {
            ModelState.AddModelError(string.Empty, localizer["The file is not a valid image."]);
            return this.StackView(model);
        }

        // Save the new avatar in the database.
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            user.AvatarRelativePath = model.AvatarUrl;
            await userManager.UpdateAsync(user);

            // Sign in the user to refresh the avatar.
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangeAvatarSuccess });
        }

        return this.StackView(model);
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public enum ManageMessageId
    {
        ChangeAvatarSuccess,
        ChangePasswordSuccess,
        ChangeProfileSuccess,
        Error
    }

    private Task<User?> GetCurrentUserAsync()
    {
        return userManager.GetUserAsync(HttpContext.User);
    }

    #endregion
}
