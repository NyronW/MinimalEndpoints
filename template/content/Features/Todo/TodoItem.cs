namespace MinimalEndpoints.Template.Features.Todo;

public class TodoItem
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool? Completed { get; set; }
}

public class TodoItemDto
{
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}