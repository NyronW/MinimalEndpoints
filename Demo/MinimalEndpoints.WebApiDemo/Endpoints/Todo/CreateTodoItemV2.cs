using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[Authorize(Policy = "todo:read-write")]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(CreateTodoItemV2), RoutePrefixOverride = "/api/v2", GroupName = "v2")]
public class CreateTodoItemV2 : Endpoint<string, IResult>
{
    private readonly ITodoRepository _repository;

    public CreateTodoItemV2(ITodoRepository repository)
    {
        _repository = repository;
    }

    public override string Pattern => "/todos";

    public override HttpMethod Method => HttpMethod.Post;

    /// <summary>
    /// This is version 2 of the create todo endpoint
    /// </summary>
    /// <param name="description">Todo description</param>
    /// <returns>New created item</returns>
    /// <remarks>
    /// This version removes the max item contraint that exists with V1 of the endpooint and also add new validation
    /// condition that enforces minimum length of 5 for todo item description
    /// </remarks>
    /// <response code="201">Returns the newly create item</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="401">Client is not authenticated</response>
    /// <response code="403">Client is forbiden</response>
    /// <response code="500">Internal server error occured</response>
    public override async Task<IResult> SendAsync(string description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return Results.BadRequest("description is required");
        }

        if (description.Length < 5)
        {
            return Results.BadRequest("description is length must be greater than or equal to five characters");
        }

        var id = await _repository.CreateAsync(description);

        return Results.Created($"/endpoints/todos/{id}", new TodoItem(id, description, false));
    }
}

