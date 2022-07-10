using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MinimalEndpoints.WebApiDemo.Endpoints;

[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Customer>))]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
[Endpoint(TagName = "Customer", OperatinId = nameof(GetAllCustomers))]
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

    /// <summary>
    /// Get all available customers
    /// </summary>
    /// <returns></returns>
    private Task<IResult> GetCustomers()
    {
        var result = Results.Ok(_customerRepository.GetAll());

        return Task.FromResult(result);
    }
}

