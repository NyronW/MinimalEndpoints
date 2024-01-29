

using FastEndpoints;
using FastEndpointsBench;

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints(
    o =>  o.Assemblies = new[]
    {
        typeof(Request).Assembly
    });

var app = builder.Build();

app.UseAuthorization();

app.UseFastEndpoints();

app.Run();

namespace FastEndpointsBench
{
    public partial class Program { }
}