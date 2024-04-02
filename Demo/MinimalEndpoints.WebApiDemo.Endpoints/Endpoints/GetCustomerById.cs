using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace MinimalEndpoints.WebApiDemo.Endpoints;
/// <summary>
/// Endpoint to get customer for a given unique identifier
/// </summary>
[Endpoint(TagName = "Customer", OperationId = nameof(GetCustomerById))]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Customer))]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public class GetCustomerById : IEndpoint
{
    private readonly ICustomerRepository _customerRepository;
    /// <summary>
    /// Contructor that accepts customer repository
    /// </summary>
    /// <param name="customerRepository"></param>
    public GetCustomerById(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public string Pattern => "/customers/{id}";

    public HttpMethod Method => HttpMethod.Get;

    public Delegate Handler => SendAsync;

    //public ValueTask<object> BindAsync(HttpRequest request, CancellationToken cancellationToken = default)
    //{
    //    var routeData = request.RouteValues["id"];

    //    if (routeData == null) return ValueTask.FromResult((object)0);

    //    var id = Convert.ChangeType(routeData, typeof(int));

    //    return ValueTask.FromResult(id!);
    //}


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
    [HandlerMethod]
    public Task<Customer> SendAsync([FromRoute] int id, Customer customer, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_customerRepository.GetById(id));
    }
}

