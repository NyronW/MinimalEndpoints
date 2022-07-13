using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
                Description = "An API to perform Customer operations",
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
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type=ReferenceType.SecurityScheme,
                            Id="Bearer"
                        }
                    },
                    new string[]{}
                }
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

        builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, DefaultAuthorizationMiddlewareResultHandler>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("todo:read-write", policyBuilder =>
            {
                policyBuilder.RequireClaim("todo:read-write", "true");
            });

            options.AddPolicy("todo:max-count", policyBuilder =>
            {
                policyBuilder.AddRequirements(new MaxTodoCountRequirement(1));
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
        });

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.UseMinimalEndpoints(o =>
        {
            o.DefaultRoutePrefix = "/api/v1";
        });

        return app;
    }
}
