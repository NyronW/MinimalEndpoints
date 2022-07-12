using FluentValidation;
using MvcControllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Logging.ClearProviders();
builder.Services.AddControllers();
builder.Services.AddSingleton<IValidator<Request>, Validator>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

namespace MvcControllers
{
    public partial class Program { }
}