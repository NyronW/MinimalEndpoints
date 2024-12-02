namespace MinimalEndpoints.Extensions;

public static class TypeExtensions
{
    public static bool DerivedFromAny(this Type type, params Type[] types)
    {
        foreach (var baseType in types)
        {
            if (baseType.IsAssignableFrom(type))
            {
                return true;
            }
        }
        return false;
    }
}