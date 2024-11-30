using Microsoft.OpenApi.Any;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public class XmlComments
{
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public string RequestBody { get; set; }
    public List<XmlCommentParameter> Parameters { get; set; } = new List<XmlCommentParameter>();
    public List<XmlCommentResponse> Responses { get; set; } = new List<XmlCommentResponse>();
}

public class XmlCommentParameter
{
    public string Name { get; set; }
    public string Description { get; set; }
    public bool Nullable { get; set; }
}

public class XmlCommentResponse
{
    public string StatusCode { get; set; }
    public string Description { get; set; }
}

public class OpenApiDecimal : OpenApiPrimitive<decimal>
{
    public OpenApiDecimal(decimal value)
        : base(value)
    {
    }

    public override PrimitiveType PrimitiveType => PrimitiveType.Float;
}