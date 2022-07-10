using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[Authorize(Policy = "todo:read-write")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperatinId = nameof(CreateTodoItem))]
public class CreateTodoItem : Endpoint<TodoItem, IResult>
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
    /// <param name="todo">Todo item</param>
    /// <returns>New created item</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /todos
    ///     {        
    ///       "id": "TSK-001",
    ///       "description": "New Task",
    ///       "completed": false        
    ///     }
    /// </remarks>
    /// <response code="201">Returns the newly create item</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="500">Internal server error occured</response>
    public override async Task<IResult> SendAsync(TodoItem todo)
    {
        if (todo == null || string.IsNullOrWhiteSpace(todo.description))
        {
            return Results.BadRequest("description is required");
        }

        var id = await _repository.CreateAsync(todo.description);

        return Results.Created($"/endpoints/todos/{id}", todo);
    }
}
