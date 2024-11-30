using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.RegularExpressions;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public class RouteTemplateCaseDocumentFilter(EndpointDescriptors endpointDescriptors) : IDocumentFilter
{
    private readonly EndpointDescriptors _endpointDescriptors = endpointDescriptors;

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var logger = _endpointDescriptors.ServiceProvider.GetRequiredService<ILogger<EndpointXmlCommentsDocumentFilter>>();
        
        try
        {
            foreach (var path in swaggerDoc.Paths.ToList())
            {
                var updatedPath = path.Key;
                bool pathUpdated = false;

                foreach (var parameter in path.Value.Operations
                                                    .SelectMany(op => op.Value.Parameters))
                {
                    var pattern = $"{{{parameter.Name}}}";

                    // Check if the exact parameter name is already in the correct case
                    if (updatedPath.Contains(pattern))
                    {
                        continue;
                    }

                    if (Regex.IsMatch(updatedPath, pattern, RegexOptions.IgnoreCase))
                    {
                        updatedPath = Regex.Replace(updatedPath, pattern, $"{{{parameter.Name}}}", RegexOptions.IgnoreCase);
                        pathUpdated = true;
                    }
                }

                if (pathUpdated && updatedPath != path.Key)
                {
                    try
                    {
                        if (swaggerDoc.Paths.TryGetValue(updatedPath, out var existingPathItem))
                        {
                            foreach (var operation in path.Value.Operations)
                            {
                                if (!existingPathItem.Operations.ContainsKey(operation.Key))
                                {
                                    existingPathItem.Operations[operation.Key] = operation.Value;
                                }
                            }

                            foreach (var server in path.Value.Servers)
                            {
                                if (!existingPathItem.Servers.Any(s => s.Url == server.Url))
                                {
                                    existingPathItem.Servers.Add(server);
                                }
                            }

                            foreach (var parameter in path.Value.Parameters)
                            {
                                if (!existingPathItem.Parameters.Any(p => p.Name == parameter.Name && p.In == parameter.In))
                                {
                                    existingPathItem.Parameters.Add(parameter);
                                }
                            }
                        }
                        else
                        {
                            swaggerDoc.Paths.Add(updatedPath, path.Value);
                        }

                        swaggerDoc.Paths.Remove(path.Key);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, $"An exception occured while attempting to update swagger ui path: '{updatedPath}'");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occured");
        }
    }
}

