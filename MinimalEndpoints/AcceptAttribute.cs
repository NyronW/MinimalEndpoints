namespace MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AcceptAttribute : Attribute
{
    public AcceptAttribute(Type type, string contentType)
    {
        Type = type;
        ContentType = contentType;
    }

    public Type Type { get; }
    public string ContentType { get; }
    public bool IsOptional { get; set; } = true;
    public string[] AdditionalContentTypes { get; set; } 
}