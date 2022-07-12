using Microsoft.AspNetCore.Authorization;

namespace MinimalEndpoints.WebApiDemo.Authorization;

public class MaxTodoCountRequirement : IAuthorizationRequirement
{
    public int MaxItems { get; set; }

    public MaxTodoCountRequirement(int maxItems)
    {
        MaxItems = maxItems;
    }
}
