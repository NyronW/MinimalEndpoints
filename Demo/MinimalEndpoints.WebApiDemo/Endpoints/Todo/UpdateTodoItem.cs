using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(UpdateTodoItem))]
public class UpdateTodoItem : IEndpoint
{
    private readonly ITodoRepository _repository;

    public UpdateTodoItem(ITodoRepository repository)
    {
        _repository = repository;
    }

    public string Pattern => "/todos/{id}";

    public HttpMethod Method => HttpMethod.Put;

    public Delegate Handler => UpdateAsync;

    /// <summary>
    /// Updates a todo item completed status
    /// </summary>
    /// <param name="id">Todo unique identifier</param>
    /// <param name="completed">Is the task completed</param>
    /// <returns></returns>
    /// <response code="200">Item updated sucessfully</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error occured</response>
    private async Task<IResult> UpdateAsync(string id, bool completed)
    {
        await _repository.Update(id, completed);

        return Results.Ok();
    }
}

