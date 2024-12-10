# MinimalEndpoints.Swashbuckle.AspNetCore
A supporting library for [MinimalEndpoints](https://www.nuget.org/packages/MinimalEndpoints) that integrates with the SwaggerGenUI for better API documentation.

### Why use MinimalEndpoints.Swashbuckle.AspNetCore?

MinimalEndpoints.Swashbuckle.AspNetCore enalbes the use of xml comment files for richer API documentation.

### Installing MinimalEndpoints.Swashbuckle.AspNetCore

You should install [MinimalEndpoints.Swashbuckle.AspNetCore with NuGet](https://www.nuget.org/packages/MinimalEndpoints.Swashbuckle.AspNetCore):

    Install-Package MinimalEndpoints.Swashbuckle.AspNetCore
    
Or via the .NET command line interface (.NET CLI):

    dotnet add package MinimalEndpoints.Swashbuckle.AspNetCore

Either commands, from Package Manager Console or .NET Core CLI, will allow download and installation of MinimalEndpoints and all its required dependencies.

### How do I get started?

First, configure MinimalEndpoints to know where the commands are located, in the startup of your application:

```csharp
//add minimal endpoint swagger support
builder.Services.AddMinimalEndpointSwaggerGen();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal Endpoint API Demo",
        Version = "v1",
        Description = "An API developed using MinimalEndpoint",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Nyron Williams",
            Url = new Uri("https://github.com/nyronw"),
        },
        License = new OpenApiLicense
        {
            Name = "Minimal Endpoint  License",
            Url = new Uri("https://example.com/license"),
        }
    });

    //Set the comments path for the Swagger JSON and UI.
    var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory)
        .Where(f => Path.GetExtension(f) == ".xml");
  

    c.IncludeXmlComments(xmlFiles);

});

```

### V1.1 Breaking Changes
* Requires that AddMinimalEndpointSwaggerGen() method be called on the IServiceCollection interface during application startup.
* Swagger UI IncludeXmlComments extension method no longer accepts an instance of EndpointDescriptor;