using Aiursoft.StatHub.Authorization;

namespace Aiursoft.StatHub.Models.PermissionsViewModels;

public class PermissionWithRoleCount
{
    public required PermissionDescriptor Permission { get; init; }
    public required int RoleCount { get; init; }
    public required int UserCount { get; init; }
}
