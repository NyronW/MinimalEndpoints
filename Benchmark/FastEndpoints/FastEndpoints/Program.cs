

using FastEndpoints;
using FastEndpointsBench;
using FluentValidation;

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints(
    o =>  o.Assemblies =
    [
        typeof(Request).Assembly
    ]);

builder.Services.AddSingleton<IValidator<Request>, Validator>();

var app = builder.Build();

app.UseAuthorization();

app.UseFastEndpoints();

app.Run();

namespace FastEndpointsBench
{
    public partial class Program { }
}