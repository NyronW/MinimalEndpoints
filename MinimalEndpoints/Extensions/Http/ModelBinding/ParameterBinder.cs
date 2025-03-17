using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace MinimalEndpoints.Extensions.Http.ModelBinding;

public static class ParameterBinder
{
    /// <summary>
    /// Bind a value from the request body as JSON.
    /// </summary>
    public static async Task<T> BindFromBodyAsync<T>(HttpContext context, T defaultValue = default)
    {
        try
        {
            // ReadFromJsonAsync returns null if the body is empty.
            var result = await context.Request.ReadFromJsonAsync<T>();
            return result != null ? result : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Bind a value from the route values.
    /// </summary>
    public static T BindFromRoute<T>(HttpContext context, string parameterName, T defaultValue = default)
    {
        if (context.Request.RouteValues.TryGetValue(parameterName, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Bind a value from the query string.
    /// </summary>
    public static T BindFromQuery<T>(HttpContext context, string parameterName, T defaultValue = default)
    {
        if (context.Request.Query.TryGetValue(parameterName, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value.ToString(), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Bind a value from the HTTP headers.
    /// </summary>
    public static T BindFromHeader<T>(HttpContext context, string headerName, T defaultValue = default)
    {
        if (context.Request.Headers.TryGetValue(headerName, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value.ToString(), typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Bind a value from DI (services).
    /// </summary>
    public static T BindFromServices<T>(HttpContext context, T defaultValue = default)
    {
        try
        {
            return context.RequestServices.GetService<T>() ?? defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Bind a parameter using default heuristics:
    /// - For primitives (and strings, decimals, etc.), try Route then Query.
    /// - For complex types, assume JSON body binding.
    /// </summary>
    public static async Task<T> BindDefaultAsync<T>(HttpContext context, string parameterName, T defaultValue = default)
    {
        // If T is a primitive, string, or decimal, try route then query.
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string) || typeof(T) == typeof(decimal))
        {
            var fromRoute = BindFromRoute(context, parameterName, defaultValue);
            // If a non-default value was found on the route, return it.
            if (!EqualityComparer<T>.Default.Equals(fromRoute, defaultValue))
                return fromRoute;

            // Otherwise, try query string.
            return BindFromQuery(context, parameterName, defaultValue);
        }
        else
        {
            // For complex types assume JSON body.
            return await BindFromBodyAsync(context, defaultValue);
        }
    }
}
