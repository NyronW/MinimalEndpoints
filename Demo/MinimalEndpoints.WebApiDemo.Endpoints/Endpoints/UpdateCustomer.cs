using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Endpoints;

internal class UpdateCustomer(ICustomerRepository repository) : IEndpointDefinition
{
    private readonly ICustomerRepository _repository = repository;

    /// <summary>
    /// Updates a customer record
    /// </summary>
    /// <param name="id">Customer Id</param>
    /// <param name="customerDto">Customer dto containing values to be saved</param>
    /// <returns></returns>
    [HandlerMethod]
    private IResult HandleCore(int id, CustomerDto customerDto)
    {
        var customer = _repository.GetById(id);
        if (customer != null)
            customer.Name = $"{customerDto.FirstName} {customerDto.LastName}";

        return Results.Ok(customer);
    }

    public RouteHandlerBuilder MapEndpoint(IEndpointRouteBuilder app)
    {
        return app.MapPut("/api/v1/customers/{id}", HandleCore)
            .WithName("UpdateCustomer")
            .WithTags("Customer")
            .Accepts<CustomerDto>("application/json", ["application/xml"]);
    }
}
