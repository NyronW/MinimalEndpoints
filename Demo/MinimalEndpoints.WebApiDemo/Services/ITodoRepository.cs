using MinimalEndpoints.WebApiDemo.Models;

namespace MinimalEndpoints.WebApiDemo.Services;


public interface ITodoRepository
{
    Task<string> CreateAsync(string name);
    Task<IEnumerable<TodoItem>> GetAllAsync();
    Task<TodoItem> Get(string id);
    Task Delete(string id);
    Task Update(string id, bool completed);
}

public class TodoRepository : ITodoRepository
{
    private readonly Dictionary<string, TodoItem> items = new Dictionary<string, TodoItem>();

    public Task<string> CreateAsync(string description)
    {
        var id = Guid.NewGuid().ToString("N");

        items.Add(id, new TodoItem(id, description, false));

        return Task.FromResult(id);
    }

    public Task Delete(string id)
    {
        items.Remove(id);

        return Task.CompletedTask;
    }

    public Task<TodoItem> Get(string id)
    {
        if (items.ContainsKey(id))
            return Task.FromResult(items[id]);

        return Task.FromResult<TodoItem>(null);
    }

    public Task<IEnumerable<TodoItem>> GetAllAsync()
    {
        var values = items.Select(i => i.Value);
        return Task.FromResult(values);
    }

    public Task Update(string id, bool completed)
    {
        if (items.ContainsKey(id))
        {
            items[id] = items[id] with { completed = completed };
        }

        return Task.CompletedTask;
    }

}


