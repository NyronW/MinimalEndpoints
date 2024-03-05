using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Xml.Linq;

namespace MinimalEndpoints.WebApiDemo;

public class EndpointXmlCommentsDocumentFilter : IDocumentFilter
{
    private readonly Dictionary<string, XmlComments> _xmlComments;
    private readonly EndpointDescriptors _endpointDescriptors;

    public EndpointXmlCommentsDocumentFilter(string xmlPath, EndpointDescriptors endpointDescriptors)
    {
        _xmlComments = XmlCommentsReader.LoadXmlComments(xmlPath);
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
                        In = ParameterLocation.Query,
                        Description = parameter.Description,
                        Required = true
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

public static class XmlCommentsReader
{
    public static Dictionary<string, XmlComments> LoadXmlComments(string xmlPath)
    {
        var comments = new Dictionary<string, XmlComments>();
        var xdoc = XDocument.Load(xmlPath);

        foreach (var member in xdoc.Descendants("member"))
        {
            var name = member.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name))
                continue;

            var summary = member.Element("summary")?.Value.Trim();
            var parameters = member.Elements("param")
                .Select(p => new XmlCommentParameter
                {
                    Name = p.Attribute("name")?.Value,
                    Description = p.Value.Trim()
                })
                .ToList();

            var responses = member.Elements("response")
                .Select(r => new XmlCommentResponse
                {
                    StatusCode = r.Attribute("code")?.Value,
                    Description = r.Value.Trim()
                })
                .ToList();

            if (!comments.ContainsKey(name))
            {
                comments.Add(name, new XmlComments
                {
                    Summary = summary,
                    Parameters = parameters,
                    Responses = responses
                });
            }
        }

        return comments;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HandlerMethodAttribute : Attribute { }

public static class HttpMethodExtensions
{
    public static OperationType ToOpenApiOperationMethod(this string httpMethod)
    {
        return httpMethod.ToUpper() switch
        {
            "GET" => OperationType.Get,
            "POST" => OperationType.Post,
            "PUT" => OperationType.Put,
            "DELETE" => OperationType.Delete,
            "PATCH" => OperationType.Patch,
            "OPTIONS" => OperationType.Options,
            "HEAD" => OperationType.Head,
            _ => throw new ArgumentOutOfRangeException(nameof(httpMethod), $"Unsupported HTTP method: {httpMethod}.")
        };
    }
}

public class XmlComments
{
    public string Summary { get; set; }
    public string Description { get; set; }
    public List<XmlCommentParameter> Parameters { get; set; } = new List<XmlCommentParameter>();
    public List<XmlCommentResponse> Responses { get; set; } = new List<XmlCommentResponse>();
}

public class XmlCommentParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
}

public class XmlCommentResponse
{
    public string StatusCode { get; set; }
    public string Description { get; set; }
}
