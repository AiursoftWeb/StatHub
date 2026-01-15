namespace Aiursoft.StatHub.Authorization;

/// <summary>
/// A fake localizer that returns the input string as is.
/// This is used to trick auto scanning tools to detect these strings for localization.
/// </summary>
public class FakeLocalizer
{
    public string this[string name] => name;
}