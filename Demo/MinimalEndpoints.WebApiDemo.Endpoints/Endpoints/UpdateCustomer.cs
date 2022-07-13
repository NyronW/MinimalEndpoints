using Microsoft.AspNetCore.Http;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Endpoints
{
    [Endpoint(TagName ="Customer")]
    public class UpdateCustomer : IEndpoint
    {
        private readonly ICustomerRepository _repository;

        public UpdateCustomer(ICustomerRepository repository)
        {
            _repository = repository;
        }

        public string Pattern => "/customers/{id}";

        public HttpMethod Method => HttpMethod.Put;

        public Delegate Handler => HandleCore;

        /// <summary>
        /// Update a customer
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private IResult HandleCore(string id, string name)
        {
            return Results.Ok();
        }
    }
}
