using MinimalEndpoints.WebApiDemo;

var builder = WebApplication.CreateBuilder(args)
        .ConfigureBuilder();

var app = builder.Build().ConfigureApplication();


app.Run();
