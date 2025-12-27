# MinimalEndpoints
A lightweight abstraction over ASP.NET Minimal API that implements [REPR (Request-Endpoint-Response) Pattern](https://deviq.com/design-patterns/repr-design-pattern).

## Table of Contents
- [Why use MinimalEndpoints?](#why-use-minimalendpoints)
- [Installation](#installing-minimalendpoints)
- [Requirements](#requirements)
- [Getting Started](#how-do-i-get-started)
- [Securing Endpoints](#how-do-i-secure-minimalendpoints)
- [OpenAPI/Swagger Support](#how-to-support-openapiswagger-with-minimalendpoints)
- [Streaming Data](#streaming-data-with-endpoints)
- [CORS](#how-do-i-enable-cors-with-minimalendpoints)
- [Rate Limiting](#how-do-i-enable-rate-limiting-with-minimalendpoints)
- [Route Prefix](#setting-route-prefix)
- [Custom Metadata Attributes](#custom-endpoint-metadata-attributes)
- [Model Binding & Content Negotiation](#model-binding--content-negotiation)
- [Asynchronous Streaming](#asynchronous-streaming-support)
- [IEndpointFilter Support](#iendpointfilter-support)
- [Version History](#version-history)

### Why use MinimalEndpoints?

MinimalEndpoints offers an alternative to Minimal API and MVC Controllers with the aim of increasing developer productivity. You get the performance of Minimal API and the benefits of MVC Controllers.

### Installing MinimalEndpoints

You should install [MinimalEndpoints with NuGet](https://www.nuget.org/packages/MinimalEndpoints):

    Install-Package MinimalEndpoints
    
Or via the .NET command line interface (.NET CLI):

    dotnet add package MinimalEndpoints

Either command, from Package Manager Console or .NET Core CLI, will allow download and installation of MinimalEndpoints and all its required dependencies.

### Requirements

- .NET 8.0 or later (LTS)
- .NET 10.0 or later (LTS) - also supported
- ASP.NET Core 8.0 or later

> **Note:** MinimalEndpoints multi-targets .NET 8.0 and .NET 10.0, ensuring compatibility with both LTS versions. This provides maximum flexibility for consumers of the library.

### How do I get started?

First, configure MinimalEndpoints to know where the endpoints are located in the startup of your application:

```csharp
var builder = WebApplication.CreateBuilder(args);

//...

// Tells MinimalEndpoints which assembly to scan for endpoints

builder.Services.AddMinimalEndpoints(typeof(MyClass));

// OR scanning multiple assemblies
builder.Services.AddMinimalEndpoints(typeof(MyService), typeof(MyEndPoint));

// Note: Calling AddMinimalEndpoints with an argument will result in the current assembly being scanned for endpoints.

//...

app.UseMinimalEndpoints();

```

Create a class that implements the [IEndpoint](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/IEndpoint.cs) interface.

> **Note:** For classes that directly implement `IEndpoint`, you must add the `[HandlerMethod]` attribute to the handler method. This attribute is automatically present on abstract methods in base classes, so you don't need to add it when inheriting from `EndpointBase` or other base classes.

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

    [HandlerMethod]
    private IResult GetCustomers()
    {
        var customers = _customerRepository.GetAll();

        return Results.Ok(customers);
    }
}
```

Version 1.4 introduced a new interface called IEndpointDefinition that offers greater control when defining an endpoint.

```csharp
public class UpdateCustomer : IEndpointDefinition
{
    private IResult HandleCore(int id, CustomerDto customerDto, [FromServices] ICustomerRepository repository)
    {
        var customer = repository.GetById(id);
        if (customer != null)
            customer.Name = $"{customerDto.FirstName} {customerDto.LastName}";

        return Results.Ok(customer);
    }

    //Implement require method
    public RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapPut("/api/v1/customers/{id}", HandleCore)
            .WithName("UpdateCustomer")
            .WithTags("Customer")
            .Accepts<CustomerDto>("application/json", ["application/xml"]);
    }
}
```
The MapEndpoint method was also added to the IEndpoint interface to facilitate customizing the endpoint configuration.

You can also inherit from the abstract base class `EndpointBase` to access helper methods that wrap many of the static methods on the `Results` class. 

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

You can also inherit your endpoints from any of the generic base classes that implement the `IEndpoint` interface:

* **[Endpoint\<TResponse\>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs)** - Used for endpoints without a request body
* **[Endpoint<TRequest, TResponse>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs)** - Used for endpoints with both a request and response
* **[GetByIdEndpoint\<TResponse\>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Endpoint.cs)** - Used for endpoints that get an object by its integer id

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

builder.Services.AddMinimalEndpoints(typeof(Program));

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
#### Enhanced Authorization Error Responses

MinimalEndpoints can return more detailed and user-friendly responses whenever there's an authorization failure. To enable this feature:

1. Call the `UseAuthorizationResultHandler()` method on the `EndpointConfiguration` class when adding MinimalEndpoints to the request pipeline
2. Pass an instance of the [EndpointAuthorizationFailureReason](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Authorization/EndpointAuthorizationFailureReason.cs) class to the `AuthorizationHandlerContext.Fail()` method call in your `AuthorizationHandler<TRequirement>` classes

You can also use the `ClaimsRequirement` class when configuring authorization to return custom messages to the client.
	
```csharp

// Startup configuration
app.UseMinimalEndpoints(o =>
{
    o.DefaultRoutePrefix = "/api/v1";
    o.DefaultGroupName = "v1";
    o.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
    o.DefaultRateLimitingPolicyName = "fixed";
    o.AddFilterMetadata(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status500InternalServerError));
    o.AddEndpointFilter<MyCustomEndpointFilter>();
    o.AddEndpointFilter(new MyCustomEndpointFilter2());
    o.AddEndpointFilter(new CorrelationIdFilter("X-Correlation-ID"));
    o.AddEndpointFilter<RequestExecutionTimeFilter>();
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

Your endpoints will be visible via Swagger with no extra effort. However, you can use the [EndpointAttribute](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/EndpointAttribute.cs) class to customize how your endpoints are exposed via Swagger.

**EndpointAttribute Properties:**

* **TagName**: Affects how your endpoints are grouped on the Swagger UI page
* **OperationId**: Used to identify each endpoint. Also used when calling `Results.CreatedAtRoute(string routeName, object routeValue)`
* **GroupName**: Assigns an endpoint to a specific Swagger document when multiple OpenAPI specifications are configured
* **ExcludeFromDescription**: Set to `true` if you don't want to list your endpoint on the Swagger UI page
* **RoutePrefixOverride**: Overrides the default route prefix if it was configured at startup
* **Description**: Provides a description for the endpoint in Swagger documentation
* **RateLimitingPolicyName**: Assigns a rate limiting policy to an endpoint. The policy must be configured in the app startup
* **RouteName**: Assigns a route name to an endpoint. This value is used when calling `Results.CreatedAtRoute(string routeName, object routeValue)`

#### XML Comments for Swagger Documentation

You can improve your endpoint documentation by using XML comments to enrich the Swagger UI. Follow the instructions from [this blog](https://code-maze.com/swagger-ui-asp-net-core-web-api/) to implement comment support.

MinimalEndpoints uses a custom `[HandlerMethod]` attribute to identify the actual method that contains the API logic. This attribute is on abstract methods in the base classes, so you do not need to add it to endpoint methods in inherited classes. However, you need to add it to the endpoint method of classes that directly implement the `IEndpoint` interface.

```csharp
[Endpoint(TagName = "Todo", OperationId = nameof(UpdateTodoItem))]
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
    /// <response code="200">Item updated successfully</response>
    /// <response code="400">Invalid data passed from client</response>
    /// <response code="404">Item not found</response>
    /// <response code="500">Internal server error occurred</response>
    [HandlerMethod]
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

You can also use the `ProducesResponseType` and `AcceptAttribute` attributes to provide details of the various HTTP status codes returned from your endpoint. You can also use the `[FromRoute]`, `[FromHeader]`, or `[FromQuery]` attributes to provide details of the route parameters.

It is recommended to add the [MinimalEndpoints.Swashbuckle.AspNetCore](https://www.nuget.org/packages/MinimalEndpoints.Swashbuckle.AspNetCore) package to your project to enhance Swagger UI integration. This package is a wrapper around the Swashbuckle.AspNetCore package and provides a more streamlined way to configure Swagger for your MinimalEndpoints application.

### Streaming Data with Endpoints

You can enable streaming responses from your endpoint using two approaches:
* Directly returning `IAsyncEnumerable<T>`
* Returning `StreamResult<T>`

The only requirement is that your data layer returns an `IAsyncEnumerable<T>`, and then you can use either of the two approaches to stream data from your endpoint.

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

You can enable CORS by adding the `EnableCors` attribute to your endpoint and configuring the CORS middleware during application startup.

**1. Configure CORS in Startup:**

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("https://example.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ...

app.UseCors("AllowSpecificOrigin");
```

**2. Apply to Endpoint:**

```csharp
[EnableCors("AllowSpecificOrigin")]
[Endpoint(TagName = "Customer", OperationId = nameof(GetAllCustomers))]
public class GetAllCustomers : IEndpoint
{
    // ...
}
```


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


### Setting Route Prefix

You can set the route prefix that is used by all your endpoints during application startup. The example below sets the default route prefix to `/api/v1`.

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

You can override the default route prefix for an endpoint by adding the `EndpointAttribute` to the endpoint and setting the `RoutePrefixOverride` property to the desired route prefix. This can be used to support endpoint versioning.

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


### Custom Endpoint Metadata Attributes

MinimalEndpoints supports automatic registration of custom metadata attributes using **default interface methods** (C# 8.0+). You can create custom attributes that implement `IEndpointMetadataAttribute` to automatically register metadata with your endpoints during registration.

> **💡 Quick Start:** Just implement `IEndpointMetadataAttribute` on your attribute class. You don't need to implement `GetMetadata()` unless you need advanced features like multiple metadata objects or transformation.

#### Why Use Custom Metadata Attributes?

Custom metadata attributes allow you to:
- Attach additional information to endpoints (caching policies, feature flags, deprecation notices, etc.)
- Access metadata at runtime via `HttpContext.GetEndpoint()?.Metadata`
- Enable middleware, filters, or documentation tools to read endpoint-specific configuration
- Keep endpoint configuration declarative and close to the endpoint definition

#### Creating Custom Metadata Attributes

**Simple Case (Most Common):**

For most use cases, you don't need to implement the `GetMetadata()` method. The interface provides a default implementation that returns the attribute itself:

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CacheAttribute : Attribute, IEndpointMetadataAttribute
{
    public int DurationSeconds { get; }
    public string? VaryByHeader { get; set; }

    public CacheAttribute(int durationSeconds)
    {
        DurationSeconds = durationSeconds;
    }

    // GetMetadata() not required - default implementation returns 'this'
}
```

**Advanced Case (When You Need Multiple Metadata Objects):**

Override `GetMetadata()` when you need to:
- Return multiple metadata objects from a single attribute
- Transform the attribute into a different metadata type
- Create metadata objects that require additional initialization

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdvancedCacheAttribute : Attribute, IEndpointMetadataAttribute
{
    public int DurationSeconds { get; }
    
    public AdvancedCacheAttribute(int durationSeconds)
    {
        DurationSeconds = durationSeconds;
    }

    // Override to return multiple metadata objects
    public IEnumerable<object> GetMetadata()
    {
        yield return this; // The attribute itself
        yield return new CachePolicyMetadata(DurationSeconds); // Additional metadata
        yield return new ResponseCacheMetadata(DurationSeconds); // Another metadata object
    }
}
```

**Why `GetMetadata()` Exists:**

The `GetMetadata()` method provides flexibility for advanced scenarios:
- **Multiple Metadata Objects**: One attribute can register multiple metadata objects
- **Transformation**: Convert attribute data into different metadata types that middleware expects
- **Conditional Metadata**: Create metadata based on attribute properties or other conditions

For 90% of use cases, the default implementation (which returns the attribute itself) is sufficient, so you don't need to implement it.

#### Using Custom Metadata Attributes

**Class-Level Attributes:**

Apply custom metadata attributes directly to your endpoint class:

```csharp
[Endpoint(TagName = "Customer", OperationId = nameof(GetAllCustomers))]
[Cache(300, VaryByHeader = "X-Client-Id")] // Automatically registered as metadata
public class GetAllCustomers : EndpointBase, IEndpoint
{
    public string Pattern => "/customers";
    public HttpMethod Method => HttpMethod.Get;
    public Delegate Handler => GetCustomers;

    [HandlerMethod]
    private IResult GetCustomers()
    {
        // ...
    }
}
```

**Method-Level Attributes:**

You can also apply custom metadata attributes to handler methods (when `IncludeMethodAttributes` is enabled in configuration):

```csharp
[Endpoint(TagName = "Customer", OperationId = nameof(GetCustomerById))]
public class GetCustomerById : IEndpoint
{
    // ...

    [HandlerMethod]
    [FeatureFlag("enable-customer-details")] // Method-level metadata
    public Task<Customer> SendAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        // ...
    }
}
```

**Multiple Attributes:**

You can apply multiple custom metadata attributes to the same endpoint:

```csharp
[Endpoint(TagName = "Customer")]
[Cache(600)] // Caching metadata
[FeatureFlag("customer-api-v2")] // Feature flag metadata
[Deprecated(Reason = "Use v2 endpoint", AlternativeEndpoint = "/api/v2/customers")] // Deprecation metadata
public class GetCustomers : EndpointBase, IEndpoint
{
    // ...
}
```

#### Configuration

Configure metadata registration in your application startup. All options have sensible defaults, so minimal configuration is needed:

```csharp
app.UseMinimalEndpoints(o =>
{
    o.DefaultRoutePrefix = "/api/v1";
    
    // Enable automatic registration of custom metadata attributes (default: true)
    // Set to false to disable automatic registration
    o.AutoRegisterMetadataAttributes = true;
    
    // Include method-level attributes in discovery (default: true)
    // Set to false to only discover class-level attributes
    o.IncludeMethodAttributes = true;
    
    // Exclude specific attribute types from auto-registration
    // Useful if you have internal attributes that shouldn't be registered
    o.ExcludedAttributeTypes.Add(typeof(SomeInternalAttribute));
});
```

> **Note:** The default configuration enables automatic registration for all attributes implementing `IEndpointMetadataAttribute`. You typically only need to configure this if you want to disable the feature or exclude specific attribute types.

#### Accessing Metadata at Runtime

Custom metadata attributes are automatically registered and can be accessed in several ways:

**1. Via HttpContext (Most Common):**

```csharp
public class CacheMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var endpoint = context.GetEndpoint();
        var cacheAttribute = endpoint?.Metadata.GetMetadata<CacheAttribute>();
        
        if (cacheAttribute != null)
        {
            // Apply caching based on attribute properties
            context.Response.Headers["Cache-Control"] = $"max-age={cacheAttribute.DurationSeconds}";
        }
        
        await next(context);
    }
}
```

**2. Via EndpointDescriptor:**

```csharp
var descriptors = serviceProvider.GetRequiredService<EndpointDescriptors>();
foreach (var descriptor in descriptors.Descriptors)
{
    var metadata = descriptor.Metadata;
    var cacheAttr = metadata?.GetMetadata<CacheAttribute>();
    // Process metadata...
}
```

**3. Via Endpoint Metadata Collection:**

```csharp
var endpoint = app.MapGet("/test", () => "Hello").Build();
var metadata = endpoint.Metadata;
var customMetadata = metadata.GetMetadata<CacheAttribute>();
```

#### Common Use Cases

Custom metadata attributes are perfect for:

- **Caching Configuration**: Define cache duration and policies per endpoint
- **Feature Flagging**: Enable/disable endpoints based on feature flags
- **Deprecation Notices**: Mark endpoints as deprecated with migration paths
- **API Documentation**: Add custom documentation metadata
- **Middleware Configuration**: Configure middleware behavior per endpoint
- **Rate Limiting**: Define rate limits per endpoint (in addition to built-in support)
- **Monitoring**: Attach monitoring tags or configuration
- **Security Policies**: Define additional security requirements

#### Quick Reference

**Simple Attribute (No `GetMetadata()` needed):**
```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MyAttribute : Attribute, IEndpointMetadataAttribute
{
    // Just implement the interface - GetMetadata() has a default implementation
}
```

**Using the Attribute:**
```csharp
[MyAttribute]
public class MyEndpoint : IEndpoint { }
```

**Accessing at Runtime:**
```csharp
var metadata = context.GetEndpoint()?.Metadata.GetMetadata<MyAttribute>();
```

**Key Points:**
- ✅ Default interface method means you don't need to implement `GetMetadata()` for simple cases
- ✅ Override `GetMetadata()` only when you need multiple metadata objects or transformation
- ✅ Works with both class-level and method-level attributes
- ✅ Automatically registered during endpoint discovery
- ✅ Uses modern C# 8.0+ default interface methods feature

### Model Binding & Content Negotiation

ASP.NET Minimal API comes with support for consuming json. If you want to support other content types, such as xml, you need custom binding logic on your models. For detailed instructions on how to implement this method see [this](https://khalidabuhakmeh.com/using-aspnet-core-mvc-value-providers-with-minimal-apis) blog. MinimalEndpoints offers a similar solution that integrates well with it's other features. You can implement the [IEndpointModelBinder](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ModelBinding/IEndpointModelBinder.cs) interface and register the class in the DI container. 

To use your new model binding capabilities, you can simply inherit your endpoints from the [EndpointBase<TRequest,TResponse>](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/EndpointBaseT.cs) class or use the `GetModelAsync` extension method on the `HttpRequest` object.

MinimalEndpoints supports both JSON and XML model binding and will throw an [EndpointModelBindingException](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ModelBinding/EndpointModelBindingException.cs) exception when an error occurs during model binding. If you inherit from the `EndpointBase<TRequest,TResponse>` class, the exception will only be sent to the caller when the environment is set to development; otherwise, it's wrapped in a [problem details](https://datatracker.ietf.org/doc/html/rfc7807) response.

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

// Using extension method to bind models

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


Content negotiation can be extended by implementing the [IResponseNegotiator](https://github.com/NyronW/MinimalEndpoints/blob/master/MinimalEndpoints/Extensions/Http/ContentNegotiation/IResponseNegotiator.cs) interface. Once you've registered your class, you can utilize the new feature by inheriting from the `EndpointBase<TRequest,TResponse>` class or using the `SendAsync` extension method on the `HttpResponse` object. MinimalEndpoints adds XML support to the existing content types already supported by ASP.NET Minimal API.

```csharp

//Calling SendAsync method will automatically negotiate the contenttype to send to client

response.Headers.Location = uri;
await response.SendAsync(model, StatusCodes.Status201Created)

```

### Asynchronous Streaming Support

Simply return a `StreamResult<T>` from your endpoint to enable streaming support. MinimalEndpoints will automatically handle the streaming of data to the client, or you can return an `IAsyncEnumerable<T>` from your endpoint method.


```csharp

    [HandlerMethod]
    public IResult SendAsync()
    {
        return new StreamResult<TodoItem>(_repository.GetAllAsyncStream());
    }

    //..OR 

    [HandlerMethod]
    public async IAsyncEnumerable<TodoItem> SendAsync()
    {
        await foreach (var item in _repository.GetAllAsyncStream())
        {
            yield return item;
        }
    }

```

### IEndpointFilter Support

You can add endpoint filters in two ways:

1. **Per-Endpoint**: Inherit from the `EndpointBase` abstract class and call the `AddEndpointFilter` method in the implementing endpoint class
2. **Global**: Add global filters via the endpoint configuration instance when calling the `UseMinimalEndpoints` method from your application startup code


```csharp

    public class UpdateTodoItem : EndpointBase, IEndpoint
    {
        private readonly ITodoRepository _repository;

        public UpdateTodoItem(ITodoRepository repository)
        {
            _repository = repository;
            AddEndpointFilter<MyCustomEndpointFilter3>();
        }

        public string Pattern => "/todos/{id}";

        public HttpMethod Method => HttpMethod.Put;

        public Delegate Handler => UpdateAsync;

        private async Task<IResult> UpdateAsync(string id, bool completed)
        {
            await _repository.Update(id, completed);

            return Results.Ok();
        }
    }


    //..OR 

        app.UseMinimalEndpoints(o =>
        {
            o.DefaultRoutePrefix = "/api/v1";
            o.DefaultGroupName = "v1";
            o.DefaultRateLimitingPolicyName = "fixed";
            o.AddFilterMetadata(new ProducesResponseTypeAttribute(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest));
            o.AddFilterMetadata(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status500InternalServerError));
            o.AddEndpointFilter<MyCustomEndpointFilter>();
            o.AddEndpointFilter(new MyCustomEndpointFilter2());
            o.AddEndpointFilter(new CorrelationIdFilter("X-Correlation-ID"));
            o.AddEndpointFilter<RequestExecutionTimeFilter>();

            o.UseAuthorizationResultHandler();
        });

```

## Version History

### V1.4.0 Changes
* Added `IEndpointDefinition` interface to allow greater flexibility when defining API endpoints
* Improved Swagger XML support by supporting more data types
* Added support for custom endpoint metadata attributes with `IEndpointMetadataAttribute`

### V1.3.0 Changes
* Added extension methods for authorization policy builder

### V1.2.9 Changes
* Added support for `IEndpointFilters`
* Added support for applying rate limiting to all minimal endpoint implementations

### V1.2.7 Changes
* Added new registration argument to enable/disable scanning of all loaded assemblies
* Added new registration method called `AddMinimalEndpointFromCallingAssembly` that only scans the calling assembly for endpoints. This enables better encapsulation of endpoints located in class libraries
* Support added to register all endpoints regardless of their access modifiers. This enables better encapsulation of endpoints located in class libraries

### V1.2 Breaking Changes
Updated abstract method definitions to accept a `CancellationToken` parameter in the following classes. This change allows for better cancellation support in the application:
* `Endpoint<TRequest, TResponse>`
* `GetByIdEndpoint<TResponse>`
* `Endpoint<TResponse>`
* `GetByIdEndpoint<TResponse, TKey>`


