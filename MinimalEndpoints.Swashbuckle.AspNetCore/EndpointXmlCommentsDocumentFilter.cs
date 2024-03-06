using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public class EndpointXmlCommentsDocumentFilter : IDocumentFilter
{
    private readonly Dictionary<string, XmlComments> _xmlComments;
    private readonly EndpointDescriptors _endpointDescriptors;

    public EndpointXmlCommentsDocumentFilter(IEnumerable<string> xmlPaths, EndpointDescriptors endpointDescriptors)
    {
        _xmlComments = XmlCommentsReader.LoadXmlComments(xmlPaths);
        _endpointDescriptors = endpointDescriptors;
    }

    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var endpointTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IEndpoint).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .ToList();

        foreach (var endpointType in endpointTypes)
        {
            var descriptor = _endpointDescriptors.Descriptors.FirstOrDefault(d => d.ClassName == endpointType.FullName);
            if (descriptor == null) continue;

            var xmlMemberName = $"M:{descriptor.HandlerMethod}";
            if (_xmlComments.TryGetValue(xmlMemberName, out var xmlComments))
            {

                if (string.IsNullOrEmpty(descriptor.Pattern) || descriptor.HttpMethod == null) continue;

                var operationType = descriptor.HttpMethod.ToOpenApiOperationMethod();

                if (!swaggerDoc.Paths.TryGetValue(descriptor.Pattern, out var pathItem))
                {
                    continue;
                }

                var operation = pathItem.Operations.FirstOrDefault(o => o.Key == operationType).Value;
                if (operation == null) continue;

                operation.Summary = xmlComments.Summary;
                operation.Description = xmlComments.Description;

                foreach (var parameter in xmlComments.Parameters)
                {
                    operation.Parameters.Add(new OpenApiParameter
                    {
                        Name = parameter.Name,
                        In = ParameterLocation.Path,
                        Description = parameter.Description,
                        Required = true,
                    });
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
        }
    }
}
