using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TodoItem>))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(GetAllTodoItemsV2), RoutePrefixOverride = "/api/v2", GroupName = "v2", RateLimitingPolicyName = "fixed")]
public class GetAllTodoItemsV2 : IEndpoint
{
    private readonly ITodoRepository _repository;

    public GetAllTodoItemsV2(ITodoRepository repository)
    {
        _repository = repository;
    }

    public string Pattern => "/todos";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => SendAsync;

    /// <summary>
    /// Gets all available todo items
    /// </summary>
    /// <returns>Returns all available todo items</returns>
    /// <response code="200">Returns all available items</response>
    /// <response code="500">Internal server error occured</response>
    [HandlerMethod]
    //public async IAsyncEnumerable<TodoItem> SendAsync()
    //{
    //    await foreach (var item in _repository.GetAllAsyncStream())
    //    {
    //        yield return item;
    //    }
    //}

    public IResult SendAsync()
    {
        return new StreamResult<TodoItem>(_repository.GetAllAsyncStream());
    }
}

