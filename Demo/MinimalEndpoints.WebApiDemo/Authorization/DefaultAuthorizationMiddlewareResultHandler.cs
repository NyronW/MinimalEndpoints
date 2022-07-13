using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace MinimalEndpoints.WebApiDemo.Authorization
{
    public class DefaultAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context,
            AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (authorizeResult.Forbidden)
            {
                var reason = authorizeResult.AuthorizationFailure?.FailureReasons.FirstOrDefault();
                var message = reason?.Message;

                if (!string.IsNullOrWhiteSpace(message))
                {
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync(message);
                    return;
                }
            }

            await defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
