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

    public GetAllCustomers(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public string Pattern => "/customers";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => GetCustomers;

    /// <summary>
    /// Get all available customers
    /// </summary>
    /// <returns></returns>
    private IResult GetCustomers()
    {
        var customers = _customerRepository.GetAll();

        return Results.Extensions.Ok(customers);
    }
}

