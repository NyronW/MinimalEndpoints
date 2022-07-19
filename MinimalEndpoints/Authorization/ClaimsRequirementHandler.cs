using Microsoft.AspNetCore.Authorization;

namespace MinimalEndpoints.Authorization;

public class ClaimsRequirementHandler : AuthorizationHandler<ClaimsRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ClaimsRequirement requirement)
    {
        var user = context.User;

        if (user == null) return Task.CompletedTask;

        if (requirement.AllowedValues.Any())
        {
            foreach (var val in requirement.AllowedValues)
            {
                if (user.HasClaim(requirement.ClaimType, val))
                {
                    context.Succeed(requirement);
                    break;
                }
            }
        }
        else
        {
            var hasClaim = user.HasClaim(claim => claim.Type
                .Equals(requirement.ClaimType, StringComparison.OrdinalIgnoreCase));

            if (hasClaim)
                context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}