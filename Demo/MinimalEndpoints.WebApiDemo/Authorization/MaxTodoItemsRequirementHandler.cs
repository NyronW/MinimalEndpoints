using Microsoft.AspNetCore.Authorization;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Authorization;

public class MaxTodoItemsRequirementHandler : AuthorizationHandler<MaxTodoCountRequirement>
{
    private readonly ITodoRepository _repository;
    private readonly IHttpContextAccessor _httpContext;

    public MaxTodoItemsRequirementHandler(ITodoRepository repository, IHttpContextAccessor httpContext)
    {
        _repository = repository;
        _httpContext = httpContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MaxTodoCountRequirement requirement)
    {
        if (requirement == null) return;

        var items = await _repository.GetAllAsync();

        if (requirement.MaxItems <= items.Count())
        {
            var instance = _httpContext?.HttpContext?.Request.Path.Value;
            var reason = new DefaultAuthorizationFailureReason(this, 
                "Maximum number of todo items reached. Please remove some items and try again", 
                instance, "https://httpstatuses.com/403",
                "Cannot add new item", 403);

            context.Fail(reason);
            return;
        }

        context.Succeed(requirement);
    }
}