using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using System.Text.Json;

namespace MinimalEndpoints.WebApiDemo.Authorization
{
    public class DefaultAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context,
            AuthorizationPolicy policy, PolicyAuthorizationResult policyAuthorization)
        {
            if (policyAuthorization.Forbidden && policyAuthorization.AuthorizationFailure != null)
            {
                if (policyAuthorization.AuthorizationFailure.FailureReasons
                    .Any(reason => typeof(IHaveProblemDetails).IsAssignableFrom(reason.GetType())))
                {
                    var reason = (IHaveProblemDetails)policyAuthorization.AuthorizationFailure
                        .FailureReasons.First(reason => typeof(IHaveProblemDetails).IsAssignableFrom(reason.GetType()));

                    var problemDetail = new
                    {
                        Type = reason.Type,
                        Title = reason.Title,
                        Detail = reason.Detail,
                        Status = reason.Status,
                        Instance = reason.Instance
                    };

                    context.Response.ContentType = "application/problem+json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetail));
                    return;
                }
                else
                {
                    var message = policyAuthorization.AuthorizationFailure.FailureReasons.FirstOrDefault()?.Message;
                    if(message is { })
                    {
                        context.Response.ContentType = "text/plain";
                        await context.Response.WriteAsync(message);
                        return;
                    }
                }
            }

            await defaultHandler.HandleAsync(next, context, policy, policyAuthorization);
        }
    }
}
