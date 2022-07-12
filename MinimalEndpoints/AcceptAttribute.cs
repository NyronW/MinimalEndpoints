namespace MinimalEndpoints;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AcceptAttribute : Attribute
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public AcceptAttribute(Type type, string contentType)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        Type = type;
        ContentType = contentType;
    }

    public Type Type { get; }
    public string ContentType { get; }
    public bool IsOptional { get; set; } = true;
    public string[] AdditionalContentTypes { get; set; } 
}