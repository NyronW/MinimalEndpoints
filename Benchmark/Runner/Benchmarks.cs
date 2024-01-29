using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Text;
using System.Text.Json;

namespace Runner;

[MemoryDiagnoser, SimpleJob(launchCount: 1, warmupCount: 1, invocationCount: 20000)]
public class Benchmarks
{
    private static HttpClient MvcClient { get; } = new WebApplicationFactory<MvcControllers.Program>().CreateClient();
    private static HttpClient MinimalEndpointClient { get; } = new WebApplicationFactory<MinimalEndpointsBench.Program>().CreateClient();
    private static HttpClient FastEndpointClient { get; } = new WebApplicationFactory<FastEndpointsBench.Program>().CreateClient();


    private static readonly StringContent Payload = new(
        JsonSerializer.Serialize(new
        {
            FirstName = "Jane",
            LastName = "Brown",
            Age = 23,
            Address = new {
                    Street = "123 Park Lane",
                    Apartment = "Apt 7",
                    City = "New Kingston",
                    State = "St. Andrew",
                    Country = "Jamaica"
            },
            PhoneNumbers = new []
            {
                "1111111111",
                "2222222222",
            },
            Email = "jane.brown@outlook.com"
        }), Encoding.UTF8, "application/json");


    [Benchmark(Baseline = true)]
    public Task AspNetCoreMVC()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{MvcClient.BaseAddress}benchmark/ok/123"),
            Content = Payload
        };

        return MvcClient.SendAsync(msg);
    }

    [Benchmark]
    public Task FastEndpoints()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{FastEndpointClient.BaseAddress}benchmark/ok/123"),
            Content = Payload
        };

        return FastEndpointClient.SendAsync(msg);
    }

    [Benchmark]
    public Task MinimalEndpoints()
    {
        var msg = new HttpRequestMessage()
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{MinimalEndpointClient.BaseAddress}benchmark/ok/123"),
            Content = Payload
        };

        return MinimalEndpointClient.SendAsync(msg);
    }
}

