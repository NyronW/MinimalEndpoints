using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[Authorize(Policy = "todo:read-write")]
[Authorize(Policy = "todo:max-count")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperatinId = nameof(CreateTodoItem))]
public class CreateTodoItem : Endpoint<string, IResult>
{
    private readonly ITodoRepository _repository;

    public CreateTodoItem(ITodoRepository repository)
    {
        _repository = repository;
    }

    public override string Pattern => "/todos";

    public override HttpMethod Method => HttpMethod.Post;

    /// <summary>
    /// Creates new todo item
    /// </summary>
    /// <param name="description">Todo description</param>
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
    public override async Task<IResult> SendAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Results.BadRequest("description is required");
        }

        var id = await _repository.CreateAsync(description);

        return Results.Created($"/endpoints/todos/{id}", new TodoItem(id, description, false));
    }
}
