using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints.Extensions.Http;

namespace MinimalEndpoints.WebApiDemo.Endpoints;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Customer>))]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized, "application/json", "application/xml")]
[Endpoint(TagName = "Customer", OperationId = nameof(GetAllCustomers))]
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

    /// <summary>
    /// Get all available customers
    /// </summary>
    /// <returns></returns>
    public Delegate Handler => GetCustomers;

    private IResult GetCustomers([FromQuery] int pageNo, [FromQuery] int pageSize = 10)
    {
        var customers = _customerRepository.GetAll();

        _someService.Foo();

        return Results.Extensions.Ok(customers);
    }
}

