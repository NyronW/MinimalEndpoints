using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MinimalEndpoints.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public class EndpointXmlCommentsDocumentFilter(IEnumerable<string> xmlPaths, EndpointDescriptors endpointDescriptors) : IDocumentFilter
{
    private readonly Dictionary<string, XmlComments> _xmlComments = XmlCommentsReader.LoadXmlComments(xmlPaths);
    private readonly EndpointDescriptors _endpointDescriptors = endpointDescriptors;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var endpointTypes = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && a.GetName().Name != null)
            .SelectMany(a => a.DefinedTypes)
            .Where(t => t.IsClass && !t.IsAbstract && t.DerivedFromAny([typeof(IEndpoint), typeof(IEndpointDefinition)]))
            .ToArray();

        var logger = _endpointDescriptors.ServiceProvider.GetRequiredService<ILogger<EndpointXmlCommentsDocumentFilter>>();

        foreach (var endpointType in endpointTypes)
        {
            try
            {
                var descriptor = _endpointDescriptors.Descriptors.FirstOrDefault(d => d.ClassName == endpointType.FullName);
                if (descriptor == null || string.IsNullOrEmpty(descriptor.Pattern) || descriptor.HttpMethod == null)
                    continue;

                if (!_xmlComments.TryGetValue($"M:{descriptor.HandlerIdentifier}", out var xmlComments))
                    continue;

                var operationType = descriptor.HttpMethod.ToOpenApiOperationMethod();
                if (!swaggerDoc.Paths.TryGetValue(descriptor.Pattern, out var pathItem))
                {
                    logger.LogDebug("Path {Pattern} not found in the swagger document", descriptor.Pattern);
                    continue;
                }

                var operation = pathItem.Operations.FirstOrDefault(o => o.Key == operationType).Value;
                if (operation == null) continue;

                operation.Summary = xmlComments.Summary;
                operation.Description = xmlComments.Description;

                MethodInfo handlerMethodInfo = endpointType.GetMethod(descriptor.HandlerMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
                if (handlerMethodInfo == null) continue;

                var schemas = context.SchemaRepository.Schemas;

                var undocumentedParams = handlerMethodInfo.GetParameters()
                        .Where(p => !xmlComments.Parameters.Any(cp => p.Name == cp.Name)
                        && p.ParameterType != typeof(CancellationToken)).ToArray();

                if (undocumentedParams.Length != 0)
                {
                    logger.LogDebug("Attempting to include parameters were not documented for {EndpointType}.{HandlerMethod}", endpointType.FullName, descriptor.HandlerMethod);
                    xmlComments.Parameters.AddRange(undocumentedParams.Select(p => new XmlCommentParameter
                    {
                        Name = p.Name!,
                        Description = string.Empty
                    }));
                }

                foreach (var parameter in xmlComments.Parameters)
                {
                    ParameterInfo? paramInfo = handlerMethodInfo.GetParameters()
                            .FirstOrDefault(p => p.Name == parameter.Name);

                    if (paramInfo == null) continue;

                    var propertyType = paramInfo.ParameterType;
                    if (propertyType == typeof(CancellationToken)) continue;
                    if (propertyType == typeof(HttpRequest)) continue;
                    if (paramInfo.GetCustomAttribute<FromServicesAttribute>() != null)
                        continue;

                    var isNullable = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>);

                    if (isNullable)
                    {
                        propertyType = Nullable.GetUnderlyingType(propertyType);
                    }

                    var openApiParameter = new OpenApiParameter
                    {
                        Name = parameter.Name,
                        Description = parameter.Description,
                        Required = !isNullable
                    };

                    var defaultValue = paramInfo.HasDefaultValue ? paramInfo.DefaultValue : null;
                    ParameterLocation? location = null!;

                    if (paramInfo.GetCustomAttribute<FromQueryAttribute>() is { } queryAttribute)
                    {
                        location = ParameterLocation.Query;
                        if (!string.IsNullOrEmpty(queryAttribute?.Name))
                        {
                            openApiParameter.Name = queryAttribute.Name;
                        }
                    }

                    if (paramInfo.GetCustomAttribute<FromHeaderAttribute>() is { } headerAttribute)
                    {
                        location = ParameterLocation.Header;
                        if (!string.IsNullOrEmpty(headerAttribute?.Name))
                        {
                            openApiParameter.Name = headerAttribute.Name;
                        }
                    }

                    if (paramInfo.GetCustomAttribute<FromRouteAttribute>() is { } routeAttribute)
                    {
                        location = ParameterLocation.Path;
                        if (!string.IsNullOrEmpty(routeAttribute?.Name))
                        {
                            openApiParameter.Name = routeAttribute.Name;
                        }
                    }


                    if (location == null)
                    {
                        if (descriptor.Pattern.Contains(parameter.Name))
                        {
                            location = ParameterLocation.Path;
                        }
                        else if (propertyType!.BaseType == typeof(ValueType) || propertyType == typeof(string))
                        {
                            location = ParameterLocation.Query;
                        }
                        else
                        {
                            if (operation.RequestBody == null && schemas.Any(s => s.Key == propertyType.Name))
                            {
                                operation.RequestBody = new OpenApiRequestBody
                                {
                                    Content = new Dictionary<string, OpenApiMediaType>
                                    {
                                        ["application/json"] = new OpenApiMediaType
                                        {
                                            Schema = new OpenApiSchema
                                            {
                                                Reference = new OpenApiReference
                                                {
                                                    Id = propertyType.Name,
                                                    Type = ReferenceType.Schema
                                                }
                                            }
                                        }
                                    }
                                };
                            }

                            continue;
                        }
                    }

                    openApiParameter.In = location;

                    openApiParameter.Schema = propertyType switch
                    {
                        var t when t!.IsArray || typeof(ICollection<>).IsAssignableFrom(t) => new OpenApiSchema
                        {
                            Type = "array",
                            Example = defaultValue != null ? new OpenApiArray() : null,
                            Default = defaultValue != null ? new OpenApiArray() : null
                        },
                        { IsEnum: true } => new OpenApiSchema
                        {
                            Type = "string",
                            Enum = Enum.GetNames(propertyType).Select(name => new OpenApiString(name)).Cast<IOpenApiAny>().ToList(),
                            Description = $"Possible values: {string.Join(", ", Enum.GetNames(propertyType))}",
                            Example = defaultValue != null ? new OpenApiArray() : null,
                            Default = defaultValue != null ? new OpenApiArray() : null
                        },
                        _ when propertyType == typeof(int) => new OpenApiSchema
                        {
                            Type = "integer",
                            Format = "int32",
                            Example = defaultValue != null ? new OpenApiInteger((int)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiInteger((int)defaultValue) : null
                        },
                        _ when propertyType == typeof(long) => new OpenApiSchema
                        {
                            Type = "integer",
                            Format = "int64",
                            Example = defaultValue != null ? new OpenApiLong((long)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiLong((long)defaultValue) : null
                        },
                        _ when propertyType == typeof(bool) => new OpenApiSchema
                        {
                            Type = "boolean",
                            Example = defaultValue != null ? new OpenApiBoolean((bool)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiBoolean((bool)defaultValue) : null
                        },
                        _ when propertyType == typeof(decimal) => new OpenApiSchema
                        {
                            Type = "number",
                            Format = "decimal",
                            Example = defaultValue != null ? new OpenApiDecimal((decimal)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiDecimal((decimal)defaultValue) : null
                        },
                        _ when propertyType == typeof(double) => new OpenApiSchema
                        {
                            Type = "number",
                            Format = "double",
                            Example = defaultValue != null ? new OpenApiDouble((double)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiDouble((double)defaultValue) : null
                        },
                        _ when propertyType == typeof(DateTime) => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "date-time",
                            Example = defaultValue != null ? new OpenApiDateTime((DateTime)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiDateTime((DateTime)defaultValue) : null
                        },
                        _ when propertyType == typeof(DateOnly) => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "date",
                            Example = defaultValue != null ? new OpenApiDate((DateTime)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiDate((DateTime)defaultValue) : null
                        },
                        _ when propertyType == typeof(TimeOnly) => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "time",
                            Example = defaultValue != null ? new OpenApiDate((DateTime)defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiDate((DateTime)defaultValue) : null
                        },
                        _ when propertyType == typeof(Guid) => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "uuid",
                            Example = defaultValue != null ? new OpenApiString(defaultValue.ToString()) : null,
                            Default = defaultValue != null ? new OpenApiString(defaultValue.ToString()) : null
                        },
                        _ when propertyType == typeof(byte) => new OpenApiSchema
                        {
                            Type = "binary",
                            Format = "byte array",
                            Example = defaultValue != null ? new OpenApiByte((byte[])defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiByte((byte[])defaultValue) : null
                        },
                        _ when propertyType == typeof(byte[]) => new OpenApiSchema
                        {
                            Type = "binary",
                            Format = "byte array",
                            Example = defaultValue != null ? new OpenApiBinary((byte[])defaultValue) : null,
                            Default = defaultValue != null ? new OpenApiBinary((byte[])defaultValue) : null
                        },
                        _ => new OpenApiSchema
                        {
                            Type = "string",
                            Example = defaultValue != null ? new OpenApiString(defaultValue.ToString()) : null,
                            Default = defaultValue != null ? new OpenApiString(defaultValue.ToString()) : null
                        }
                    };

                    int index = operation.Parameters.Select((item, i) => new { item, i })
                                .FirstOrDefault(x => x.item.Name == openApiParameter.Name)?.i ?? -1;

                    if (index == -1)
                        operation.Parameters.Add(openApiParameter);
                    else
                    {
                        operation.Parameters[index] = openApiParameter;
                    }
                }

                foreach (var response in xmlComments.Responses)
                {
                    if (operation.Responses.ContainsKey(response.StatusCode)) continue;

                    operation.Responses.Add(response.StatusCode, new OpenApiResponse
                    {
                        Description = response.Description
                    });
                }
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "An error occured while applying xml comments to the swagger document");
            }
        }
    }
}