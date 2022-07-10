using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MinimalEndpoints.WebApiDemo.Endpoints;

[Endpoint(TagName = "Customer", OperatinId = nameof(GetCustomerById))]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Customer))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public class GetCustomerById : GetByIdEndpoint<IResult>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerById(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public override string Pattern => "/customers/{id:int}";

    /// <summary>
    /// Get customer by unique identifier
    /// </summary>
    /// <param name="id">Customer unique identifier</param>
    /// <returns>A newly created TodoItem</returns>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /customers/1
    /// </remarks>
    /// <response code="200">Returns the customer for specified id</response>
    /// <response code="404">Customer not found</response>
    public override Task<IResult> SendAsync(int id)
    {
        var customer = _customerRepository.GetById(id);

        if (customer == null) return Task.FromResult(NotFound());

        return Task.FromResult(Ok(customer));
    }
}

