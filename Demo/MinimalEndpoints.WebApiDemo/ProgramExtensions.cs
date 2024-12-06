using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalEndpoints.WebApiDemo.Authorization;
using MinimalEndpoints.WebApiDemo.Endpoints;
using MinimalEndpoints.WebApiDemo.Services;
using MinimalEndpoints.Swashbuckle.AspNetCore;
using System.Text;
using System.Threading.RateLimiting;
using MinimalEndpoints.Extensions.Http;
using MinimalEndpoints.Authorization;

namespace MinimalEndpoints.WebApiDemo;

public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ITodoRepository, TodoRepository>();


        builder.Services.AddMinimalOpenApi();
        builder.Services.AddMinimalEndpoints();

        builder.Services.AddCustomerServices(); //Add services for support class library

        builder.Services.AddControllers();

        builder.Services.AddHttpContextAccessor();

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
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
                    //Email = "nyronwilliams@gmail.com",
                    Url = new Uri("https://github.com/nyronw"),
                },
                License = new OpenApiLicense
                {
                    Name = "Minimal Endpoint  License",
                    Url = new Uri("https://example.com/license"),
                }
            });

            c.SwaggerDoc("v2", new OpenApiInfo
            {
                Title = "Minimal Endpoint API Demo",
                Version = "v2",
                Description = "Version 2 of API developed using MinimalEndpoint",
                TermsOfService = new Uri("https://example.com/terms"),
                Contact = new OpenApiContact
                {
                    Name = "Nyron Williams",
                    //Email = "nyronwilliams@gmail.com",
                    Url = new Uri("https://github.com/nyronw"),
                },
                License = new OpenApiLicense
                {
                    Name = "Minimal Endpoint  License",
                    Url = new Uri("https://example.com/license"),
                }
            });

            c.OperationFilter<SecureSwaggerEndpointhRequirementFilter>();

            // Set the comments path for the Swagger JSON and UI.
            var xmlFiles = Directory.GetFiles(AppContext.BaseDirectory)
                .Where(f => Path.GetExtension(f) == ".xml");

            //foreach (var xmlFile in xmlFiles)
            //    c.IncludeXmlComments(xmlFile);

            c.IncludeXmlComments(xmlFiles);

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

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(c =>
        {
            var key = Encoding.ASCII.GetBytes(builder.Configuration["AuthZ:SecretKey"]);

            c.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["AuthZ:Issuer"],
                ValidAudience = builder.Configuration["AuthZ:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });

        builder.Services.AddTransient<IAuthorizationHandler, MaxTodoItemsRequirementHandler>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("todo:read-write", policyBuilder =>
            {
                policyBuilder.RequireScope("todo:read-write", "true");
            });

            options.AddPolicy("todo:max-count", policyBuilder =>
            {
                policyBuilder.AddRequirements(new MaxTodoCountRequirement(0));
            });
        });

        builder.Services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: "fixed", options =>
            {
                options.PermitLimit = 4;
                options.Window = TimeSpan.FromSeconds(5);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            }));

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Minimal Endpoint API Demo");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "Minimal Endpoint API Demo (V2)");
        });

        app.UseRateLimiter();

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.UseMinimalEndpoints(o =>
        {
            o.DefaultRoutePrefix = "/api/v1";
            o.DefaultGroupName = "v1";
            o.DefaultRateLimitingPolicyName = "fixed";
            o.AddFilterMetadata(new ProducesResponseTypeAttribute(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest));
            o.AddFilterMetadata(new ProducesResponseTypeAttribute(typeof(ProblemDetails), StatusCodes.Status500InternalServerError));
            o.AddEndpointFilter<MyCustomEndpointFilter>();
            o.AddEndpointFilter(new MyCustomEndpointFilter2());
            o.AddEndpointFilter(new CorrelationIdFilter("X-Correlation-ID"));
            o.AddEndpointFilter<RequestExecutionTimeFilter>();

            o.UseAuthorizationResultHandler();
        });

        return app;
    }
}

public sealed class MyCustomEndpointFilter(ILogger<MyCustomEndpointFilter> logger) : IEndpointFilter
{
    private readonly ILogger<MyCustomEndpointFilter> _logger = logger;

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        _logger.LogInformation("Before from MyCustom filter");
        var result = await next(context);
        _logger.LogInformation("After from MyCustom filter");

        return result;
    }
}


public sealed class MyCustomEndpointFilter2 : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<MyCustomEndpointFilter2>>();

        logger.LogInformation("Before from MyCustom2 filter");
        var result = await next(context);
        logger.LogInformation("After from MyCustom2 filter");
        return result;
    }
}