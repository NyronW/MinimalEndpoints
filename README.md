# MinimalEndpoints
A light weight abstraction over ASP.Net Minimal API that implements [REPR (Request-Endpoint-Response) Pattern](https://deviq.com/design-patterns/repr-design-pattern).

### Why use MinimalEndpoints?

MinimalEndpoints offers an alternative to the Minimal Api and MVC Controllers with the aim of increasing developer productivity. You get the performance Minimal Api and benefits of MVC Controllers.

### Installing MinimalEndpoints

You should install [MinimalEndpoints with NuGet](https://www.nuget.org/packages/MinimalEndpoints):

    Install-Package MinimalEndpoints
    
Or via the .NET command line interface (.NET CLI):

    dotnet add package MinimalEndpoints

Either commands, from Package Manager Console or .NET Core CLI, will allow download and installation of MinimalEndpoints and all its required dependencies.

### How do I get started?

First, configure MinimalEndpoints to know where the commands are located, in the startup of your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

//...

// Tells MinimalEndpoints which assembly to scan for endpoints

builder.Services.AddMinimalEndpoints(typeof(MyClass));

//OR Scanning multiple assemblies
builder.Services.AddMinimalEndpoints(typeof(MyService), typeof(MyEndPoint));

Calling AddMinimalEndpoints with an argument will  result in the current assembly being scanned for endpoints

//...

app.UseMinimalEndpoints();

```

Create a class that implements the [IEndpoint](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/IEndpoint.cs) interface.

```csharp
public class GetAllCustomers : IEndpoint
{
    private readonly ICustomerRepository _customerRepository;

    public GetAllCustomers(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public string Pattern => "/customers";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => GetCustomers;

    private IResult GetCustomers()
    {
        var customers = _customerRepository.GetAll();

        return Results.Ok(customers);
    }
}
```

You can also implement the abstract base classes <em>EndpointBase</em> to access helper methods that wraps alot of the static methods on the Results class. 

```csharp
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

    private async Task<IResult> DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest("id is required"); // EndpointBase wrapper for Results.BadRequest(object?).
        }

        var todo = await _repository.Get(id);

        if (todo == null) return NotFound(); // EndpointBase wrapper method

        await _repository.Delete(id);

        return Ok();// EndpointBase wrapper method
    }
}
```

You can also inherit your endpoints from any of generic classes that implements the IEndPoint interface.

* [Endpoint\<TResponse\>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs) is used for endpoints without a request.

* [Endpoint<TRequest, TResponse>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs) is used for endpoints with both a request and response

* [GetByIdEndpoint\<TResponse\>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs) is used for endpoints that getting an object by its integer id

```csharp

public class GetCustomerById : GetByIdEndpoint<Customer>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerById(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public override string Pattern => "/customers/{id:int}";

    public override Task<Customer> SendAsync(int id)
    {
        return Task.FromResult(_customerRepository.GetById(id));
    }
}
```

### How do I secure MinimalEndpoints?

MinimalEndpoints leverages existing ASP.NET Authorization features and requires little effort for integration. 

```csharp

//...

builder.Services.AddWebMinimalEndpoints();

//...

//Adding JWT Token support
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(c =>
{
	var key = Encoding.ASCII.GetBytes(builder.Configuration["AuthZ:SecretKey"]);

	c.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["AuthZ:Issuer"],
		ValidAudience = builder.Configuration["AuthZ:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(key)
	};
});

builder.Services.AddTransient<IAuthorizationHandler, MaxTodoItemsRequirementHandler>();

builder.Services.AddAuthorization(options =>
{
	//Adding sample policies
	options.AddPolicy("todo:read-write", policyBuilder =>
	{
		policyBuilder.RequireClaim("todo:read-write", "true");
	});

	options.AddPolicy("todo:max-count", policyBuilder =>
	{
		policyBuilder.AddRequirements(new MaxTodoCountRequirement(5));
	});
});


//Add the Authorize attribute to the endpoint
[Authorize(Policy = "todo:read-write")]
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

```

### How to support OpenAPI/Swagger with MinimalEndpoints?

Your endpoints will be visible via Swagger with no extra effort, however you can used the [EndpointAttribute](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/EndpointAttribute.cs) class to customize how your endpoints are exposed via Swagger.

* TagName: This property affects how your endpoints are grouped on the Swagger UI page.
* OperationId: This propery is used to identitfy each endpoint. This is also used when creating calling Results.CreateAtRoute(string routeName, object routeValue).
* ExcludeFromDescription: Set this property to true if you want don't want to list your endpoint on the Swagger UI page
* RoutePrefixOverride: This property is used to override the default route prefix, if it was configured at startup.

You can improve your endpoint documentation by using comments to enrich the Swagger UI. You can follow the instructions from [this](https://code-maze.com/swagger-ui-asp-net-core-web-api/) blog to implement cooment support. 



```csharp

[Endpoint(TagName = "Todo", OperatinId = nameof(UpdateTodoItem))]
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
    /// <param name="todo">Todo item to be updated</param>
    /// <returns></returns>
    /// <response code="200">Item updated sucessfully</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error occured</response>
    private async Task<IResult> UpdateAsync(string id, TodoItem todo)
    {
        if (todo == null || !todo.completed.HasValue)
        {
            return Results.BadRequest("completed is required");
        }

        await _repository.Update(id, todo.completed.Value);

        return Results.Ok();
    }
}

//...

```

You can also use the ProducesResponseType attribute to provide details of the various HTTP codes return from your endpoint.

### How do I enable CORS with MinimalEndpoints?

You can simply add the EnableCors attribute to your endpoint and add the CORS middleware during your application startup.

### Setting route prefix?

You can set the route prefix that is used by all your endpoint during application startup. The example below sets the defautl route prefix to /api/v1.

```csharp
//...
var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

//..
app.UseMinimalEndpoints(o =>
{
    o.DefaultRoutePrefix = "/api/v1";
});

```

You can override the default route prefix for an enpoint by adding the EndpointAttribute to the endpoint and setting the RoutePrefixOverride property to the desired route prefix. This can be used to support endpoint versioning.

```csharp
[Endpoint(TagName = "Todo", OperationId = nameof(CreateTodoItemV2), RoutePrefixOverride = "/api/v2")]
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
    public override async Task<IResult> SendAsync(string description)
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

```
