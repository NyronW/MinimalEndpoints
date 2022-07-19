using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Extensions;
using MinimalEndpoints.Extensions.Http;

namespace MinimalEndpoints.Authorization;

public class EndpointAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();
    private readonly IHttpContextAccessor _contextAccessor;

    public EndpointAuthorizationMiddlewareResultHandler(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public async Task HandleAsync(RequestDelegate next, HttpContext httpContext,
        AuthorizationPolicy policy, PolicyAuthorizationResult policyAuthorizationResult)
    {
        if (EndpointConfiguration.UseEndpointAuthorizationMiddlewareResultHandler &&  policyAuthorizationResult.Forbidden 
            && policyAuthorizationResult.AuthorizationFailure != null)
        {
            if (policyAuthorizationResult.AuthorizationFailure.FailureReasons
                .Any(reason => reason is IHaveProblemDetails))
            {
                var reason = (IHaveProblemDetails)policyAuthorizationResult.AuthorizationFailure
                    .FailureReasons.First(reason => reason is IHaveProblemDetails);

                var problemDetail = new ProblemDetails
                {
                    Type = reason.Type,
                    Title = reason.Title,
                    Detail = reason.Detail,
                    Status = reason.Status,
                    Instance = reason.Instance
                };

                await httpContext.Response.SendAsync(problemDetail, StatusCodes.Status403Forbidden, "application/problem+");
                return;
            }
            else
            {
                var message = policyAuthorizationResult.AuthorizationFailure.FailureReasons.FirstOrDefault()?.Message;
                if (message is { })
                {
                    var problemDetail = new ProblemDetails
                    {
                        Type = "https//httpstatuses.com/403",
                        Title = "Request failed authorization checks",
                        Detail = message,
                        Status = StatusCodes.Status403Forbidden,
                        Instance = _contextAccessor?.HttpContext?.Request.Path.Value
                    };

                    await httpContext.Response.SendAsync(problemDetail, StatusCodes.Status403Forbidden, "application/problem+");
                    return;
                }
            }

            if (policyAuthorizationResult.AuthorizationFailure.FailedRequirements.Any(requirement => requirement is ClaimsRequirement))
            {
                var claimRequirement = (ClaimsRequirement)policyAuthorizationResult.AuthorizationFailure
                    .FailedRequirements.First(requirement => requirement is ClaimsRequirement);

                var problemDetail = new ProblemDetails
                {
                    Type = claimRequirement.Type,
                    Title = claimRequirement.Title,
                    Detail = claimRequirement.Detail,
                    Status = StatusCodes.Status403Forbidden,
                    Instance = _contextAccessor?.HttpContext?.Request.Path.Value
                };

                await httpContext.Response.SendAsync(problemDetail, StatusCodes.Status403Forbidden, "application/problem+");
                return;
            }
        }

        await defaultHandler.HandleAsync(next, httpContext, policy, policyAuthorizationResult);
    }
}