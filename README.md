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

//Adding JWT Token support (this is just for demo use)
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
MinimalEndpoint can return a more detailed and user friendly response whenever there's an authorization failure. To enable this feature call the UseAuthorizationResultHandler method on the EndpointConfiguration class when adding MinimalEndpoint to the request pipeline. The next step is to pass an instance of the [EndpointAuthorizationFailureReason](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Authorization/EndpointAuthorizationFailureReason.cs) class to the AuthorizationHandlerContext.Fail() method call in your AuthorizationHandler<TRequirement> classes. You can also use the ClaimsRequirement class when configuring authorization to return custom messages to the client.
	
```csharp

//start up configuration
app.UseMinimalEndpoints(o =>
{
    o.DefaultRoutePrefix = "/api/v1";
    o.DefaultGroupName = "v1";
    o.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
    o.UseAuthorizationResultHandler();
});

//Authorization handler

public class MaxTodoItemsRequirementHandler : AuthorizationHandler<MaxTodoCountRequirement>
{
    private readonly ITodoRepository _repository;
    private readonly IHttpContextAccessor _httpContext;

    public MaxTodoItemsRequirementHandler(ITodoRepository repository, IHttpContextAccessor httpContext)
    {
        _repository = repository;
        _httpContext = httpContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MaxTodoCountRequirement requirement)
    {
        if (requirement == null) return;

        var items = await _repository.GetAllAsync();

        if (requirement.MaxItems <= items.Count())
        {
            var instance = _httpContext?.HttpContext?.Request.Path.Value;
            var reason = new EndpointAuthorizationFailureReason(this, 
                "Maximum number of todo items reached. Please remove some items and try again", 
                instance, "https://httpstatuses.com/403",
                "Cannot add new item", 403);

            context.Fail(reason);
            return;
        }

        context.Succeed(requirement);
    }
}	
	
//Using custom ClaimRequirement
						 
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("todo:read-write", policyBuilder =>
    {
	policyBuilder.RequireClaim("todo:read-write", "true"); // default claims configuration
    });

    options.AddPolicy("todo:max-count", policyBuilder =>
    {
	policyBuilder.AddRequirements(new ClaimsRequirement("todo:read-write2",
	    "You do not have permission to create, update or delete todo items",
	    allowedValues: new[] { "true" })); //custom claim configuration
	policyBuilder.AddRequirements(new MaxTodoCountRequirement(1));
    });
});						 
	
```
### How to support OpenAPI/Swagger with MinimalEndpoints?

Your endpoints will be visible via Swagger with no extra effort, however you can used the [EndpointAttribute](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/EndpointAttribute.cs) class to customize how your endpoints are exposed via Swagger.

* TagName: This property affects how your endpoints are grouped on the Swagger UI page.
* OperationId: This propery is used to identitfy each endpoint. This is also used when creating calling Results.CreateAtRoute(string routeName, object routeValue).
* GroupName: This property is used to assign an endpoint to a specific Swagger document when multiple Open API soecifications are configured
* ExcludeFromDescription: Set this property to true if you want don't want to list your endpoint on the Swagger UI page
* RoutePrefixOverride: This property is used to override the default route prefix, if it was configured at startup.
* Filters: Use this property to add filters to all endpoints. Only the ProducesResponseType attribute is currently supported for global filters

You can improve your endpoint documentation by using comments to enrich the Swagger UI. You can follow the instructions from [this](https://code-maze.com/swagger-ui-asp-net-core-web-api/) blog to implement cooment support. MinimalEndpoints uses a custom attribute [HandlerMethod] to identify
the actual method that contains the API logic. This attribute is on abstract method on the base classes so you do not need to add it to the endpoint method on inherited classes, however, you need to a it to the endpoint method of classes that directly implements the IEndpoint interface.



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

### Streaming data with Endpoints?

You can enable streaming response from your endpoint using two approaches:
* Directly returning IAsyncEnumerable<T>
* returning StreamResult<T> 

Only requirement is that your data layer return an IAsyncEnumerable<T> and then you are able to use either of the two approaches to stream data from your endpoint.

```csharp
//...
[HandlerMethod]
public async IAsyncEnumerable<TodoItem> SendAsync()
{
    await foreach (var item in _repository.GetAllAsyncStream())
    {
        yield return item;
    }
}

//..
[HandlerMethod]
public IResult SendAsync()
{
    return new StreamResult<TodoItem>(_repository.GetAllAsyncStream());
}
```

### How do I enable CORS with MinimalEndpoints?

You can simply add the EnableCors attribute to your endpoint and add the CORS middleware during your application startup.


### How do I enable Rate Limiting with MinimalEndpoints?

First you need to setup rate limiting feature in the app startup and add one or more policies and then you assign the policy to the endpoint attribute
on the endpoint class.

```csharp
//...
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    }));

//...
app.UseRateLimiter();

//...
[Endpoint(TagName = "Todo", OperationId = nameof(GetAllTodoItems), RateLimitingPolicyName = "fixed")]
public class GetAllTodoItems : IEndpoint
{
    private readonly ITodoRepository _repository;

    public GetAllTodoItems(ITodoRepository repository)
    {
        _repository = repository;
    }

    public string Pattern => "/todos";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => SendAsync;
   //...
}

```


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


### Model Binding & Content Negotiation

ASP.NET Minimal API comes with support for consuming json. If you want to support other content types, such as xml, you need custom binding logic on your models. For detailed instructions on how to implement this method see [this](https://khalidabuhakmeh.com/using-aspnet-core-mvc-value-providers-with-minimal-apis) blog. MinimalEndpoints offers a similar solution that integrates well with it's other features. You can implement the [IEndpointModelBinder](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ModelBinding/IEndpointModelBinder.cs) interface and register the class in the DI container. 

To use your new model binding capabilities you can simply inherit your endpoints fromt the [EnpointBase<TRequest,TResponse>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/EndpointBaseT.cs) class or use the GetModelAsync extension method on the HttpRequest object. MinimalEndpoints supports both json and xml model binding and will throw an [EndpointModelBindingException](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ModelBinding/EndpointModelBindingException.cs) exception when an error occurs during model binding. If you inherit from the EndpointBase<TRequest,TResponse> class the exception will only be sent to the caller when the environment is set to development, otherwise its wraped in the [problem details](https://datatracker.ietf.org/doc/html/rfc7807) response.

```csharp
//Implement model binding contract
public class XmlEndpointModelBinder : IEndpointModelBinder
{
    public bool CanHandle(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) && contentType.Contains("xml", StringComparison.OrdinalIgnoreCase);

    public async ValueTask<TModel?> BindAsync<TModel>(HttpRequest request, CancellationToken cancellationToken)
    {
        TModel? model = default;

        if (request.HasXmlContentType())
            model = await request.ReadFromXmlAsync<TModel>(cancellationToken);

        return model;
    }
}

//Inheriting from EndpointBase<TRequest,TResponse>

//Use the accept attribute to tell clients what content types are allowed. This example accepts json and xml
[Accept(typeof(CustomerDto), "application/json", AdditionalContentTypes = new[] { "application/xml" })]
[Endpoint(TagName = "Customer", OperationId = nameof(CreateCustomer))]
public class CreateCustomer : EndpointBase<CustomerDto, Customer>
{
	private readonly ICustomerRepository _repository;

	public CreateCustomer(ILoggerFactory loggerFactory, ICustomerRepository repository) : base(loggerFactory)
	{
	    _repository = repository;
	}

	public override string Pattern => "/customers";

	public override HttpMethod Method => HttpMethod.Post;

	public override async Task<IResult> HandleRequestAsync(CustomerDto customerDto, HttpRequest httpRequest, CancellationToken cancellationToken = default)
	{
	    try
	    {
		var customer = await _repository.CreateAsync(customerDto);

		return CreatedAtRoute(nameof(GetCustomerById), new { id = customer.Id }, customer);
	    }
	    catch
	    {
		//custom error handling or throw for base class to handle
		throw;
	    }
  	}
}

//Using extenion method to bind models

CustomerDto? model = await httpRequest.GetModelAsync<CustomerDto>(cancellationToken);

```

You can also implement the BindAsync method on the IEndpoint interface, this will enable you to use custom data binding logic in your application.

```csharp

public class GetCustomerById : GetByIdEndpoint<Customer>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerById(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public override string Pattern => "/customers/{id:int}";


    public ValueTask<object> BindAsync(HttpRequest request, CancellationToken cancellationToken = default)
    {
        var routeData = request.RouteValues["id"];

        if (routeData == null) return ValueTask.FromResult((object)0);

        var id = Convert.ChangeType(routeData, typeof(int));

        return ValueTask.FromResult(id!);
    }

    public override Task<Customer> SendAsync(int id)
    {
        return Task.FromResult(_customerRepository.GetById(id));
    }
}

```


Content negotiation can be extended by implementing the [IResponseNegotiator](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ContentNegotiation/IResponseNegotiator.cs) interface. Once you've regitered your class, you can utalize the new feature by inheriting from the EndpointBase<TRequest,TResponse> class or using the SendAsync extension method on the HttpResponse object. MinimalEndpoint adds xml support to the existing content types already supported by ASP.NET Minimal API.

```csharp

//Calling SendAsync method will automatically negotiate the contenttype to send to client

response.Headers.Location = uri;
await response.SendAsync(model, StatusCodes.Status201Created)

```

### Rate Limiting support

