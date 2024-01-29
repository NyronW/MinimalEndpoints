using Microsoft.Extensions.Logging;

namespace MinimalEndpoints.Extensions;

public static class LoggerExtensions
{
    public static IDisposable? AddContext<T>(this ILogger logger, string id, T? value)
    {
        return logger.BeginScope(new Dictionary<string, object?>
        {
            [id] = value
        });
    }
}