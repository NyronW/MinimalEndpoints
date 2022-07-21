using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints;

namespace MinimalEndpoints.Template.Features.Todo;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TodoItem>))]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(GetAllTodoItems))]
public class GetAllTodoItems : IEndpoint
{
    private readonly ITodoRepository _repository;

    public GetAllTodoItems(ITodoRepository repository)
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
    public async Task<IEnumerable<TodoItem>> SendAsync()
    {
        return await _repository.GetAllAsync();
    }
}

