using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.WebApiDemo.Endpoints
{
    public interface ISomeService
    {
        void Foo();
    }

    public class SomeService : ISomeService
    {
        private readonly IServiceProvider _serviceProvider;

        public SomeService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Foo()
        {
            var rep = _serviceProvider.GetService<ICustomerRepository>();

            Console.WriteLine("Foo method called");
        }
    }
}
