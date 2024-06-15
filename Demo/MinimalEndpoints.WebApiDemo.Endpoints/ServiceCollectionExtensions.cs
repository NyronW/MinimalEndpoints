using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.WebApiDemo.Endpoints;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerServices(this IServiceCollection services)
    {
        services.AddMinimalEndpointFromCallingAssembly();

        services.AddTransient<ISomeService, SomeService>();

        services.AddSingleton<ICustomerRepository, CustomerRepository>();

        return services;
    }
}
