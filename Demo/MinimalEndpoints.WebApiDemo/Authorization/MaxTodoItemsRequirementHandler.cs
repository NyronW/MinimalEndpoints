using Microsoft.AspNetCore.Authorization;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Authorization;

public class MaxTodoItemsRequirementHandler : AuthorizationHandler<MaxTodoCountRequirement>
{
    private readonly ITodoRepository _repository;

    public MaxTodoItemsRequirementHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MaxTodoCountRequirement requirement)
    {
        if (requirement == null) return;

        var items = await _repository.GetAllAsync();

        if (requirement.MaxItems <= items.Count())
        {
            context.Fail(new AuthorizationFailureReason(this,"Maximum number of todo items reached. Please remove some items and try again"));
            return;
        }
        
        context.Succeed(requirement);
    }
}