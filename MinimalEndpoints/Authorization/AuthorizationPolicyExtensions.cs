using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace MinimalEndpoints.Authorization;

public static class AuthorizationPolicyExtensions
{
    public static AuthorizationPolicyBuilder RequireScope(this AuthorizationPolicyBuilder policyBuilder, params string[] requiredScopes)
    {
        policyBuilder.RequireAssertion(context =>
        {
            var scopeClaim = context.User.Claims
                .FirstOrDefault(c => c.Type == "scope")?.Value;

            if (scopeClaim != null)
            {
                var scopes = scopeClaim.Split(' ');
                return requiredScopes.Any(scope => scopes.Contains(scope));
            }

            return false; 
        });

        return policyBuilder;
    }

    public static AuthorizationPolicyBuilder RequireAnyRole(this AuthorizationPolicyBuilder policyBuilder, params string[] roles)
    {
        policyBuilder.RequireAssertion(context =>
        {
            var userRoles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value);

            return roles.Any(role => userRoles.Contains(role));
        });

        return policyBuilder;
    }

    /// <summary>
    /// Require a specific claim to have one of several values.
    /// </summary>
    /// <param name="policyBuilder"></param>
    /// <param name="claimType">The claim type required</param>
    /// <param name="values"></param>
    /// <returns>A reference to this instance when oeration is completed</returns>
    public static AuthorizationPolicyBuilder RequireClaimValue(this AuthorizationPolicyBuilder policyBuilder, string claimType, params string[] values)
    {
        policyBuilder.RequireAssertion(context =>
        {
            var claimValue = context.User.Claims
                .FirstOrDefault(c => c.Type == claimType)?.Value;

            return values.Contains(claimValue);
        });

        return policyBuilder;
    }
    public static AuthorizationPolicyBuilder RequireCustomHeader(this AuthorizationPolicyBuilder policyBuilder, string headerName, string expectedValue)
    {
        policyBuilder.RequireAssertion(context =>
        {
            var httpContext = context.Resource as HttpContext;
            return httpContext != null && httpContext.Request.Headers[headerName].FirstOrDefault() == expectedValue;
        });

        return policyBuilder;
    }
}

