using Aiursoft.StatHub.Models;

namespace Aiursoft.StatHub.Configuration;

public class SettingsMap
{
    public const string AlertCenterEnabled = "AlertCenter_Enabled";
    public const string AlertCenterEndpoint = "AlertCenter_Endpoint";
    public const string AlertCenterApiKey = "AlertCenter_ApiKey";
    public const string AlertCenterPublicBaseUrl = "AlertCenter_PublicBaseUrl";
    public const string ProjectName = "ProjectName";
    public const string BrandName = "BrandName";
    public const string BrandHomeUrl = "BrandHomeUrl";
    public const string ProjectLogo = "ProjectLogo";
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string Icp = "Icp";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = AlertCenterEnabled,
            Name = Localizer["Enable AlertCenter synchronization"],
            Description = Localizer["Synchronize server health incidents with AlertCenter."],
            Type = SettingType.Bool,
            DefaultValue = "False"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterEndpoint,
            Name = Localizer["AlertCenter endpoint"],
            Description = Localizer["The HTTP(S) base URL of AlertCenter."],
            Type = SettingType.Text,
            DefaultValue = "https://alertcenter.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterApiKey,
            Name = Localizer["AlertCenter API key"],
            Description = Localizer["An application API key with query, create, change severity, and mitigate permissions."],
            Type = SettingType.Secret,
            DefaultValue = ""
        },
        new GlobalSettingDefinition
        {
            Key = AlertCenterPublicBaseUrl,
            Name = Localizer["StatHub public base URL"],
            Description = Localizer["The public base URL of this StatHub instance, used in incident links."],
            Type = SettingType.Text,
            DefaultValue = "https://stathub.aiursoft.com"
        },

        new GlobalSettingDefinition
        {
            Key = ProjectName,
            Name = Localizer["Project Name"],
            Description = Localizer["The name of the project displayed in the frontend."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft StatHub"
        },
        new GlobalSettingDefinition
        {
            Key = BrandName,
            Name = Localizer["Brand Name"],
            Description = Localizer["The brand name of the company or project. E.g. Aiursoft."],
            Type = SettingType.Text,
            DefaultValue = "Aiursoft"
        },
        new GlobalSettingDefinition
        {
            Key = BrandHomeUrl,
            Name = Localizer["Brand Home URL"],
            Description = Localizer["The URL of the company or project. E.g. https://www.aiursoft.com"],
            Type = SettingType.Text,
            DefaultValue = "https://www.aiursoft.com"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectLogo,
            Name = Localizer["Project Logo"],
            Description = Localizer["The logo of the project displayed in the navbar and footer. Support jpg, png, svg."],
            Type = SettingType.File,
            DefaultValue = "",
            Subfolder = "project-logo",
            AllowedExtensions = "jpg png svg",
            MaxSizeInMb = 5
        },
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = Localizer["Allow User Adjust Nickname"],
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = Icp,
            Name = Localizer["ICP Number"],
            Description = Localizer["The ICP license number for China mainland users. Leave empty to hide."],
            Type = SettingType.Text,
            DefaultValue = ""
        }
    };
}
