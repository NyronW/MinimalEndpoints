using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

//[Authorize(Policy = "todo:read-write")]
//[Authorize(Policy = "todo:max-count")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(CreateTodoItem), RouteName = nameof(CreateTodoItem))]
public class CreateTodoItem : IEndpoint
{
    private readonly ITodoRepository _repository;

    public CreateTodoItem(ITodoRepository repository)
    {
        _repository = repository;
    }

    public  string Pattern => "/todos";

    public HttpMethod Method => HttpMethod.Post;

    public Delegate Handler => SendAsync;

    /// <summary>
    /// Creates new todo item
    /// </summary>
    /// <param name="description">Description of task</param>
    /// <returns>New created item</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /todos
    ///     {        
    ///       "description": "New Task",
    ///     }
    /// </remarks>
    /// <response code="201">Returns the newly create item</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="401">Client is not authenticated</response>
    /// <response code="403">Client is forbiden</response>
    /// <response code="500">Internal server error occured</response>
    public async Task<IResult> SendAsync(string description, [FromQuery] bool? foo, [FromQuery] string? bar, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Results.BadRequest("description is required");
        }

        var id = await _repository.CreateAsync(description);

        return Results.Extensions.CreatedAtRoute(nameof(GetTodoById), new { id }, new TodoItem(id, description, false));
    }
}
