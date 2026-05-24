using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace VCloset.Infrastructure.Security;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options) { }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionCode = policyName.Substring(RequirePermissionAttribute.PolicyPrefix.Length);

            var policy = new AuthorizationPolicyBuilder();
            policy.AddRequirements(new PermissionRequirement(permissionCode));
            return policy.Build();
        }

        return await base.GetPolicyAsync(policyName);
    }
}