using Microsoft.AspNetCore.Mvc;
using MinimalEndpoints;
#if (AddSwagger)
using Microsoft.OpenApi.Models;
#endif
using MinimalEndpoints.Template.Features.Todo;

namespace MinimalEndpoints.Template;

public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        builder.Services.AddMinimalEndpoints();

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddEndpointsApiExplorer();
#if (AddSwagger)
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MinimalEndpoints.Template API",
                Version = "v1",
                Description = "An API developed using MinimalEndpoint",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = "Author Name",
                    Url = new Uri("https://github.com/nyronw"),
                },
                License = new OpenApiLicense
                {
                    Name = "MinimalEndpoints.Template License",
                    Url = new Uri("https://example.com/license"),
                }
            });

            c.OperationFilter<SecureSwaggerEndpointRequirementFilter>();

            // Set the comments path for the Swagger JSON and UI.
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory)
                .Where(f => Path.GetExtension(f) == ".xml");

            foreach (var xmlFile in xmlFiles)
            {
                c.IncludeXmlComments(xmlFile);
            }

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Please enter a valid token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                BearerFormat = "JWT",
                Scheme = "Bearer"
            });
        });
#endif

        builder.Services.AddSingleton<ITodoRepository, TodoRepository>();

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
#if (AddSwagger)        
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "MinimalEndpoints.Template API");
        });
#endif
        app.UseHttpsRedirection();

        app.UseMinimalEndpoints(options =>
        {
            options.DefaultRoutePrefix = "/api/v1";
            options.DefaultGroupName = "v1";
            options.Filters.Add(new ProducesResponseTypeAttribute(typeof(HttpValidationProblemDetails), StatusCodes.Status400BadRequest, "application/problem+"));
        });

        return app;
    }
}
