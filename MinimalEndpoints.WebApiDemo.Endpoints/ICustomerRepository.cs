namespace MinimalEndpoints.WebApiDemo.Endpoints;

public interface ICustomerRepository
{
    IEnumerable<Customer> GetAll();
    Customer GetById(int id);
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


    public IEnumerable<Customer> GetAll()
    {
        return _customerList;
    }

    public Customer GetById(int id)
    {
        return _customerList.SingleOrDefault(c => c.Id == id);
    }
}
