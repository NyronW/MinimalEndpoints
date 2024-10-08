﻿using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.WebApiDemo.Models;
using MinimalEndpoints.WebApiDemo.Services;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Todo;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TodoItem))]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(GetTodoById), RouteName = nameof(GetTodoById))]
public class GetTodoById : Endpoint<string, TodoItem>
{
    private readonly ITodoRepository _repository;

    public GetTodoById(ITodoRepository repository)
    {
        _repository = repository;
    }

    public override string Pattern => "/todos/{id}";

    public override HttpMethod Method => HttpMethod.Get;

    /// <summary>
    /// Get todo item by id
    /// </summary>
    /// <param name="id">Item unique id</param>
    /// <returns></returns>
    public override async Task<TodoItem> SendAsync([FromRoute] string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)) Results.BadRequest("id is required");

        var todo = await _repository.Get(id);

        if (todo == null) Results.NotFound();

        return todo;
    }

    public async ValueTask<object[]> BindAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var id = request.RouteValues["id"].ToString();

        await Task.CompletedTask;
        return new object[] { id };
    }
}

