namespace MinimalEndpoints.WebApiDemo.Endpoints;

public interface ICustomerRepository
{
    Customer[] Get(int pageNo = 1, int pageSize = 10);
    Customer? GetById(int id);
    Task<Customer> CreateAsync(CustomerDto customer);
}

public class CustomerRepository : ICustomerRepository
{
    private List<Customer> _customerList =
    [
        new Customer(1,"Nyron Williams"),
        new Customer(2,"Hilary James") ,
        new Customer(3, "Winsome Parker"),
        new Customer(4, "Sarah Jones"),
        new Customer(5, "Cassandre Lee")
    ];

    public Task<Customer> CreateAsync(CustomerDto customer)
    {
        var id = _customerList.Count + 1;
        var newCustomer = new Customer { Id = id, Name = $"{customer.FirstName} {customer.LastName}" };

        _customerList.Add(newCustomer);

        return Task.FromResult(newCustomer);
    }

    public Customer[] Get(int pageNo = 1, int pageSize = 10)
    {
        return _customerList.Skip((pageNo - 1) * pageSize).Take(pageSize)
            .ToArray();
    }

    public Customer? GetById(int id)
    {
        return _customerList.SingleOrDefault(c => c.Id == id);
    }
}
