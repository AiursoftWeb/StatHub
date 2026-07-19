using System.Net;
using Aiursoft.StatHub.Services;

using Aiursoft.StatHub.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
namespace Aiursoft.StatHub.Tests.IntegrationTests;

[TestClass]
public class ManageControllerTests : TestBase
{
    [TestMethod]
    public async Task TestManageWorkflow()
    {
        await LoginAsAdmin();

        // Ensure AllowUserAdjustNickname is true
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(Configuration.SettingsMap.AllowUserAdjustNickname, "True");
        }

        // 1. Index
        var indexResponse = await Http.GetAsync("/Manage/Index");
        indexResponse.EnsureSuccessStatusCode();

        // 2. ChangePassword (GET)
        var changePasswordPage = await Http.GetAsync("/Manage/ChangePassword");
        changePasswordPage.EnsureSuccessStatusCode();

        // 3. ChangePassword (POST)
        var changePasswordResponse = await PostForm("/Manage/ChangePassword", new Dictionary<string, string>
        {
            { "OldPassword", "Admin@123456!" },
            { "NewPassword", "NewAdmin123!" },
            { "ConfirmPassword", "NewAdmin123!" }
        });
        AssertRedirect(changePasswordResponse, "/Manage?Message=ChangePasswordSuccess");

        // 4. ChangeProfile (GET)
        var changeProfilePage = await Http.GetAsync("/Manage/ChangeProfile");
        changeProfilePage.EnsureSuccessStatusCode();

        // 5. ChangeProfile (POST)
        var newName = "New Admin Name";
        var changeProfileResponse = await PostForm("/Manage/ChangeProfile", new Dictionary<string, string>
        {
            { "Name", newName }
        });
        AssertRedirect(changeProfileResponse, "/Manage?Message=ChangeProfileSuccess");

        // 6. ChangeAvatar (GET)
        var changeAvatarPage = await Http.GetAsync("/Manage/ChangeAvatar");
        changeAvatarPage.EnsureSuccessStatusCode();
        
        // Skip ChangeAvatar POST as it requires a valid image file in a physical path or a complex mock of StorageService.
    }

    [TestMethod]
    public async Task TestChangeAvatarAllowsJpegExtension()
    {
        await LoginAsAdmin();

        var response = await Http.GetAsync("/Manage/ChangeAvatar");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("data-allowed-file-extensions=\"png bmp jpg jpeg\"", html);
        Assert.Contains("validExtensions: ('png bmp jpg jpeg' || '').split(' ').filter(Boolean)", html);
    }

    [TestMethod]
    public async Task TestChangeProfileDisabled()
    {
        await LoginAsAdmin();

        // Disable AllowUserAdjustNickname
        using (var scope = Server!.Services.CreateScope())
        {
            var settingsService = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
            await settingsService.UpdateSettingAsync(Configuration.SettingsMap.AllowUserAdjustNickname, "False");
        }

        var changeProfilePage = await Http.GetAsync("/Manage/ChangeProfile");
        Assert.AreEqual(HttpStatusCode.BadRequest, changeProfilePage.StatusCode);

        var changeProfileResponse = await PostForm("/Manage/ChangeProfile", new Dictionary<string, string>
        {
            { "Name", "Some Name" }
        });
        Assert.AreEqual(HttpStatusCode.BadRequest, changeProfileResponse.StatusCode);
    }

    [TestMethod]
    public async Task TestDeleteAccount_Authenticated_DeletesUserAndSignsOut()
    {
        // Arrange: register and login as a new user
        var (email, _) = await RegisterAndLoginAsync();

        // Act: confirmation page loads
        var deletePage = await Http.GetAsync("/Manage/DeleteAccount");
        deletePage.EnsureSuccessStatusCode();

        // Act: confirm deletion
        var deleteResponse = await PostForm("/Manage/DeleteAccountPost", new(), tokenUrl: "/Manage/DeleteAccount");
        AssertRedirect(deleteResponse, "/");

        // Assert: signed out — accessing a protected page should redirect to login
        var managePage = await Http.GetAsync("/Manage/Index");
        Assert.AreEqual(HttpStatusCode.Found, managePage.StatusCode);

        // Assert: user no longer exists in the database
        using var scope = Server!.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Assert.IsNull(await userManager.FindByEmailAsync(email));
    }

    [TestMethod]
    public async Task TestDeleteAccount_Unauthenticated_RedirectsToLogin()
    {
        // No login — direct access should redirect
        var deletePage = await Http.GetAsync("/Manage/DeleteAccount");
        Assert.AreEqual(HttpStatusCode.Found, deletePage.StatusCode);
    }
}
