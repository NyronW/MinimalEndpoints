using FluentValidation;
using MinimalEndpoints;
using MinimalEndpointsBench;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Services.AddMinimalEndpoints();
builder.Services.AddSingleton<IValidator<Request>, Validator>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();


app.Run();

namespace MinimalEndpointsBench
{
    public partial class Program { }
}
