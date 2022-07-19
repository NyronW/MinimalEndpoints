using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MinimalEndpoints.WebApiDemo.Authorization;
using MinimalEndpoints.WebApiDemo.Endpoints;
using MinimalEndpoints.WebApiDemo.Services;
using System.Reflection;
using System.Text;

namespace MinimalEndpoints.WebApiDemo;

public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ITodoRepository, TodoRepository>();

        builder.Services.AddMinimalEndpoints(typeof(ITodoRepository).Assembly, typeof(ICustomerRepository).Assembly);

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
                policyBuilder.RequireClaim("todo:read-write", "true");
            });

            options.AddPolicy("todo:max-count", policyBuilder =>
            {
                policyBuilder.AddRequirements(new MaxTodoCountRequirement(0));
            });
        });

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

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.UseMinimalEndpoints(o =>
        {
            o.DefaultRoutePrefix = "/api/v1";
            o.DefaultGroupName = "v1";
            o.Filters.Add(new ProducesResponseTypeAttribute(StatusCodes.Status400BadRequest));
            o.UseAuthorizationResultHandler();
        });

        return app;
    }
}
