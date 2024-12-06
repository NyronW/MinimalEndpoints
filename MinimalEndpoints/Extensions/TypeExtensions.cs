namespace MinimalEndpoints.Extensions;

public static class TypeExtensions
{
    public static bool DerivedFromAny(this Type type, IReadOnlyList<Type> types)
    {
        for (int i = 0; i < types.Count; i++)
        {
            if (types[i].IsAssignableFrom(type))
            {
                return true;
            }
        }
        return false;
    }

}