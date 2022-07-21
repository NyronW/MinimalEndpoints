namespace MinimalEndpoints.Template.Features.Todo;

public interface ITodoRepository
{
    Task<string> CreateAsync(TodoItemDto model);
    Task<IEnumerable<TodoItem>> GetAllAsync();
}

public class TodoRepository : ITodoRepository
{
    private readonly Dictionary<string, TodoItem> items = new()
    {
        { "000001", new TodoItem { Id = "000001", Title = "Create project", Description = "Download useful class libraries that will be used in application", Completed = false } },
        { "000002", new TodoItem { Id = "000002", Title = "Add new features", Description = "Add new folder in the Featues directory and implement new logic in the folder", Completed = false } },
    };

    public Task<string> CreateAsync(TodoItemDto model)
    {
        var id = Guid.NewGuid().ToString("N");

        items.Add(id, new TodoItem { Id = id, Title = model.Title, Description = model.Description, Completed = false });

        return Task.FromResult(id);
    }

    public Task<IEnumerable<TodoItem>> GetAllAsync()
    {
        var values = items.Select(i => i.Value);
        return Task.FromResult(values);
    }
}

