

using FastEndpoints;

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Services.AddHttpContextAccessor();
builder.Services.AddFastEndpoints();

var app = builder.Build();

app.UseAuthorization();

app.UseFastEndpoints();

app.Run();

namespace FastEndpointsBench
{
    public partial class Program { }
}