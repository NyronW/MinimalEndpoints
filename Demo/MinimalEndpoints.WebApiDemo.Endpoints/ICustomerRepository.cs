namespace MinimalEndpoints.WebApiDemo.Endpoints;

public interface ICustomerRepository
{
    IEnumerable<Customer> GetAll();
    Customer GetById(int id);
    Task<Customer> CreateAsync(CustomerDto customer);
}

public class CustomerRepository : ICustomerRepository
{
    private List<Customer> _customerList = new List<Customer>
            {
                new Customer(1,"Nyron Williams"),
                new Customer(2,"Hilary James") ,
                new Customer(3, "Winsome Parker"),
                new Customer(4, "Sarah Jones"),
                new Customer(5, "Cassandre Lee")
            };

    public Task<Customer> CreateAsync(CustomerDto customer)
    {
        var id = _customerList.Count + 1;
        var newCustomer = new Customer { Id = id, Name = $"{customer.FirstName} {customer.LastName}" };

        _customerList.Add(newCustomer);

        return Task.FromResult(newCustomer);
    }

    public IEnumerable<Customer> GetAll()
    {
        return _customerList;
    }

    public Customer GetById(int id)
    {
        return _customerList.SingleOrDefault(c => c.Id == id);
    }

}
