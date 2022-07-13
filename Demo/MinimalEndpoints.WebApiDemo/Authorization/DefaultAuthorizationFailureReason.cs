using Microsoft.AspNetCore.Authorization;

namespace MinimalEndpoints.WebApiDemo.Authorization;

public class DefaultAuthorizationFailureReason : AuthorizationFailureReason, IHaveProblemDetails
{
    public DefaultAuthorizationFailureReason(IAuthorizationHandler handler, string message, string? instance = null, 
        string? type = null, string? title = null,  int? status = null) : base(handler, message)
    {
        Type = type ?? "https//httpstatuses.com/403";
        Status = status ?? 403;
        Title = title ?? "Request failed authorization checks";
        Instance = instance ?? string.Empty;
    }

    public string Type { get; }

    public string Detail => Message;

    public string Title { get; }

    public string Instance { get; }

    public int Status { get; }
}