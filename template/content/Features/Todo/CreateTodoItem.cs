using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.Extensions.Validation;

namespace MinimalEndpoints.Template.Features.Todo;

[Accept(typeof(TodoItemDto), "application/json", AdditionalContentTypes = new[] { "application/xml" })]
[ProducesResponseType(typeof(TodoItem), StatusCodes.Status201Created, "application/json", "application/xml")]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Todo", OperationId = nameof(CreateTodoItem))]
public class CreateTodoItem : EndpointBase<TodoItemDto, IResult>
{
    private readonly ITodoRepository _repository;

    public CreateTodoItem(ILoggerFactory loggerFactory, ITodoRepository repository): base(loggerFactory)
    {
        _repository = repository;
    }

    public override string Pattern => "/todos";

    public override HttpMethod Method => HttpMethod.Post;

    /// <summary>
    /// Creates new todo item
    /// </summary>
    /// <param name="description">Todo item</param>
    /// <returns>New created item</returns>
    /// <remarks>
    /// Sample request:
    ///     POST /todos
    ///     {        
    ///       "title": "New Task",
    ///       "description": "This is a detail description"
    ///     }
    /// </remarks>
    /// <response code="201">Returns the newly create item</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="500">Internal server error occured</response>
    protected override Task<IResult> HandlerCore(HttpRequest httpRequest, CancellationToken cancellationToken = default)
    {
        //Override this method to enable swagger to display xml comment (above)
        //If you're not using swagger or don't require api description
        return base.HandlerCore(httpRequest, cancellationToken);
    }

    public override async Task<IResult> HandleRequestAsync(TodoItemDto model, HttpRequest httpRequest, CancellationToken cancellationToken = default)
    {
        var id = await _repository.CreateAsync(model);

        //use content negotiation
        return Results.Extensions.Created($"/todos/{id}", new TodoItem { Id = id, Title = model.Title, Description = model.Description, Completed = false });
    }

    //This check can be moved to an external validator library such as FluentValidation
    public override Task<IEnumerable<ValidationError>> ValidateAsync(TodoItemDto model)
    {
        var errors = new List<ValidationError>();

        if (model == null) errors.Add(new ValidationError("", "Missing or invalid data"));

        if (string.IsNullOrEmpty(model?.Title)) errors.Add(new ValidationError(nameof(model.Title), "Title is required"));

        if (string.IsNullOrEmpty(model?.Description)) errors.Add(new ValidationError(nameof(model.Description), "Description is required"));

        return Task.FromResult(errors.AsEnumerable());
    }
}


