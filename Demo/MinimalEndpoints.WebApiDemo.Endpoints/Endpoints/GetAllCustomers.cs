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

    public string Pattern => "~/api-prod/customers";

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

public sealed class ListCurrenciesEndpoint : IEndpoint
{
    public string Pattern => "/reference/currencies";
    public HttpMethod Method => HttpMethod.Get;
    public Delegate Handler => Handle;

    /// <summary>
    /// test
    /// </summary>
    /// <param name="tenantId"></param>
    /// <param name="all"></param>
    /// <param name="includeInactive"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    [HandlerMethod]
    private async Task<IResult> Handle([FromQuery] Guid? tenantId, [FromQuery] bool? all, [FromQuery] bool? includeInactive, CancellationToken ct)
    {
        // ?all=true: return all active currencies (e.g. admin dropdown when adding a currency to a tenant)
        if (all == true)
        {
            return Results.Ok(new { items = "" });
        }
        // Resolve tenant: explicit query param or current user's tenant (branch app / tenant config)
        var resolvedTenantId = tenantId ?? GetClaimTenantId();
        if (resolvedTenantId.HasValue)
        {
            return Results.Ok(new { items = "" });
        }
        // No tenant context: return all (e.g. global admin)
        return Results.Ok(new { items = "" });
    }

    private Guid? GetClaimTenantId() => Guid.NewGuid();
}