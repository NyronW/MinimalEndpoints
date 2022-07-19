using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MinimalEndpoints.Extensions;

namespace MinimalEndpoints.Authorization;

public class ClaimsRequirement : IAuthorizationRequirement, IHaveProblemDetails
{
    public ClaimsRequirement(string claimType, string errorMessage, string? problemType = null,
        string? title = null, params string[] allowedValues)
    {
        Type = problemType ?? "https//httpstatuses.com/403";
        ClaimType = claimType;
        Detail = errorMessage;
        Status = StatusCodes.Status403Forbidden;
        Title = title ?? "Request failed authorization checks";
        AllowedValues = allowedValues;
        Instance = string.Empty;
    }

    public string Type { get; }

    public string ClaimType { get; }

    public string Detail { get; }

    public string Title { get; }

    public string[] AllowedValues { get; }

    public string Instance { get; }

    public int Status { get; }
}
