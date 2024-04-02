using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TodoItem>))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Customer", OperationId = nameof(GetAllCustomerTodoItems), RateLimitingPolicyName = "fixed")]
public class GetAllCustomerTodoItems : IEndpoint
{
    private readonly ITodoRepository _repository;

    public GetAllCustomerTodoItems(ITodoRepository repository)
    {
        _repository = repository;
    }

    public string Pattern => "/customers/{customerId}todos";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => SendAsync;

    /// <summary>
    /// Gets all available todo items
    /// </summary>
    /// <param name="customerId">Customer id</param>
    /// <returns>Returns all available todo items</returns>
    /// <response code="200">Returns all available items</response>
    /// <response code="500">Internal server error occured</response>
    [HandlerMethod]
    public async Task<IEnumerable<TodoItem>> SendAsync([FromRoute(Name ="Customer")] int customerId = 1)
    {
        return await _repository.GetAllAsync();
    }
}

