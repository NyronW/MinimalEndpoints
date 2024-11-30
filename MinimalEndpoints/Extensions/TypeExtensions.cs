namespace MinimalEndpoints.Extensions;

public static class TypeExtensions
{
    public static bool DerivedFromAny(this Type type, params Type[] types)
    {
        var invalidTypes = types.Where(m => m.IsAssignableFrom(type)).ToList();
        return invalidTypes.Any();
    }
}