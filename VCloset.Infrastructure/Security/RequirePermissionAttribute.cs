using Microsoft.AspNetCore.Authorization;

namespace VCloset.Infrastructure.Security;

public class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission_";

    public RequirePermissionAttribute(string permissionCode)
    {
        Policy = $"{PolicyPrefix}{permissionCode}";
    }
}