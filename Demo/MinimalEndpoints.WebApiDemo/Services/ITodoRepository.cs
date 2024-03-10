using MinimalEndpoints.WebApiDemo.Models;

namespace MinimalEndpoints.WebApiDemo.Services;


public interface ITodoRepository
{
    Task<string> CreateAsync(string name);
    Task<IEnumerable<TodoItem>> GetAllAsync();
    IAsyncEnumerable<TodoItem> GetAllAsyncStream();
    Task<TodoItem> Get(string id);
    Task Delete(string id);
    Task Update(string id, bool completed);
}

public class TodoRepository : ITodoRepository
{
    private readonly Dictionary<string, TodoItem> items = new Dictionary<string, TodoItem>
    {
        ["1"] = new TodoItem("1", "Buy groceries", true),
        ["2"] = new TodoItem("2", "Call Mom", true),
        ["3"] = new TodoItem("3", "Finish coding", false),
        ["4"] = new TodoItem("4", "Clean the kitchen", false),
        ["5"] = new TodoItem("5", "Read a book", false),
        ["6"] = new TodoItem("6", "Update resume", false),
        ["7"] = new TodoItem("7", "Plan holiday", false),
        ["8"] = new TodoItem("8", "Schedule dentist appointment", false),
        ["9"] = new TodoItem("9", "Pay bills", false),
        ["10"] = new TodoItem("10", "Prepare for meeting", false)
    };

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

        return Task.FromResult<TodoItem>(null!);
    }

    public Task<IEnumerable<TodoItem>> GetAllAsync()
    {
        var values = items.Select(i => i.Value);
        return Task.FromResult(values);
    }

    public async IAsyncEnumerable<TodoItem> GetAllAsyncStream()
    {
        var todoItems = items.Select(i => i.Value);
        foreach (var item in todoItems)
        {
            await Task.Delay(500);//simulate slow IO operation
            yield return item;
        }
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

