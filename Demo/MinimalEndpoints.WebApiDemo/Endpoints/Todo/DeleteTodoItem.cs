using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[Authorize(Policy = "todo:read-write")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(DeleteTodoItem))]
public class DeleteTodoItem : EndpointBase, IEndpoint
{
    private readonly ITodoRepository _repository;

    public DeleteTodoItem(ITodoRepository repository)
    {
        _repository = repository;
    }

    public string Pattern => "/todos/{id}";

    public HttpMethod Method => HttpMethod.Delete;

    public Delegate Handler => DeleteAsync;

    /// <summary>
    /// Remove a todo item
    /// </summary>
    /// <param name="id">Todo item unique identifier</param>
    /// <returns></returns>
    /// <response code="200">Item updated sucessfully</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="401">Client is not authenticated</response>
    /// <response code="403">Client is forbiden</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error occured</response>
    private async Task<IResult> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required");
        }

        var todo = await _repository.Get(id);

        if (todo == null) return NotFound();

        await _repository.Delete(id);

        return Ok();
    }
}
