using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using MinimalEndpoints.Extensions;

namespace MinimalEndpoints.Authorization;

public class EndpointAuthorizationFailureReason : AuthorizationFailureReason, IHaveProblemDetails
{
    public EndpointAuthorizationFailureReason(IAuthorizationHandler handler, string message, string? instance = null,
        string? type = null, string? title = null, int? status = null) : base(handler, message)
    {
        Type = type ?? "https//httpstatuses.com/403";
        Status = status ?? StatusCodes.Status403Forbidden;
        Title = title ?? "Request failed authorization checks";
        Instance = instance ?? string.Empty;
    }

    public string Type { get; }

    public string Detail => Message;

    public string Title { get; }

    public string Instance { get; }

    public int Status { get; }
}