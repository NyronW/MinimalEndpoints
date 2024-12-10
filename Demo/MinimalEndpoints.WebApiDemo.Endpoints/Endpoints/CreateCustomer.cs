using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinimalEndpoints.Extensions.Validation;

namespace MinimalEndpoints.WebApiDemo.Endpoints.Endpoints
{
    /// <summary>
    /// Creates a new customer 
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Customer))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(ProblemDetails))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
    [Accept(typeof(CustomerWrapper<CustomerDto>), "application/json", AdditionalContentTypes = ["application/xml"])]
    [Endpoint(TagName = "Customer", OperationId = nameof(CreateCustomer))]
    public class CreateCustomer : EndpointBase, IEndpoint
    {
        private readonly ICustomerRepository _repository;

        /// <summary>
        /// Construct endpoint class
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="repository"></param>
        public CreateCustomer(ILoggerFactory loggerFactory, ICustomerRepository repository)
        {
            _repository = repository;
        }

        public string Pattern => "/customers";

        public HttpMethod Method => HttpMethod.Post;

        public Delegate Handler => HandleRequestAsync;

        /// <summary>
        /// Create new customer
        /// </summary>
        /// <param name="customerDto">New customer to create</param>
        /// <returns></returns>
        [HandlerMethod]
        public async Task<IResult> HandleRequestAsync(CustomerWrapper<CustomerDto> customerDto, [FromHeader(Name ="X-FOO")] string clientId, HttpRequest httpRequest, CancellationToken cancellationToken = default)
        {
            try
            {
                var customer = await _repository.CreateAsync(customerDto.Customer);

                return CreatedAtRoute(nameof(GetCustomerById), new { id = customer.Id }, customer);
            }
            catch
            {
                //custom error handling or throw for base class to handle
                throw;
            }
        }

        public Task<IEnumerable<ValidationError>> ValidateAsync(CustomerDto request)
        {
            //This check can be moved to an external validator library such as FluentValidation

            var errors = new List<ValidationError>();

            if (request == null) errors.Add(new ValidationError("", "Missing or invalid data"));

            if (string.IsNullOrEmpty(request?.FirstName)) errors.Add(new ValidationError("FirstName", "Firstname is required"));

            if (string.IsNullOrEmpty(request?.LastName)) errors.Add(new ValidationError("LastName", "Lastname is required"));

            return Task.FromResult(errors.AsEnumerable());
        }
    }
}

public class CustomerWrapper<TCustomer> where TCustomer : class
{
    public int Seq { get; set; }
    public TCustomer Customer { get; set; }
}