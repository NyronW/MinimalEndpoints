using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.WebApiDemo.Endpoints.Attributes;

namespace MinimalEndpoints.WebApiDemo.Endpoints;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Customer>))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/json", "application/xml")]
[Endpoint(TagName = "Customer", Description = "Description from attributes", OperationId = nameof(GetAllCustomers))]
[Cache(300, VaryByHeader = "X-Client-Id")] // Example: Class-level custom metadata attribute for caching
public class GetAllCustomers : EndpointBase, IEndpoint
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ISomeService _someService;

    public GetAllCustomers(ICustomerRepository customerRepository, ISomeService someService)
    {
        _customerRepository = customerRepository;
        _someService = someService;
    }

    public string Pattern => "/customers";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => GetCustomers;

    /// <summary>
    /// Get all available customers
    /// </summary>
    /// <param name="pageNo">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="name">name of argument</param>
    /// <param name="showInactive"></param>
    /// <param name="customer"></param>
    /// <returns>All customers</returns>
    [HandlerMethod]
    private IResult GetCustomers([FromQuery] CustomerType category,  [FromQuery] int pageNo, [FromQuery(Name ="size")] int pageSize, [FromHeader(Name ="x-foo-name")] string name, [FromQuery] bool? showInactive)
    {
        var customers = _customerRepository.Get(pageNo   , pageSize);

        _someService.Foo();

        return Results.Extensions.Ok(customers);
    }
}

/// <summary>
/// Customer category
/// </summary>
public enum CustomerType
{
    Premium,
    Standard
}