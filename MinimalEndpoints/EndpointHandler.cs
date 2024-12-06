using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalEndpoints.Extensions;
using MinimalEndpoints.Extensions.Http;
using System.Collections.Concurrent;
using System.Reflection;

namespace MinimalEndpoints;

public class EndpointHandler
{
    private const string _endpointCorrIdName = "EndpointName";

    private readonly ILogger<EndpointHandler> _logger;
    private readonly ConcurrentDictionary<Type, (bool IsOverridden, MethodInfo? Method)> _bindingCache;
    private readonly ConcurrentDictionary<MethodInfo, MethodDetails> _methodCache;
    private readonly ConcurrentDictionary<Type, object?> _valueTypeInstances;

    private readonly Func<IEndpoint, IServiceProvider, ILoggerFactory, HttpRequest, CancellationToken, Task<object?>> _handler;

    public EndpointHandler(ILogger<EndpointHandler> logger, ConcurrentDictionary<Type, object?> valueTypeInstances,
        ConcurrentDictionary<MethodInfo, MethodDetails> methodCache,
        ConcurrentDictionary<Type, (bool IsOverridden, MethodInfo? Method)> bindingCache)
    {
        _handler = CreateHandler();
        _logger = logger;
        _valueTypeInstances = valueTypeInstances;
        _methodCache = methodCache;
        _bindingCache = bindingCache;
    }

    private Func<IEndpoint, IServiceProvider, ILoggerFactory, HttpRequest, CancellationToken, Task<object?>> CreateHandler()
    {
        return async (endpoint, sp, loggerFactory, request, cancellationToken) =>
        {
            object? result = null!;
            var endpointName = endpoint.GetType().FullName;
            using var _ = _logger.AddContext(_endpointCorrIdName, endpointName);

            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Executing endpoint: '{EndpointName}'", endpointName);
                }

                var ep = (IEndpoint)sp.GetService(endpoint.GetType())!;

                var (methodInfo, parameters) = GetMethodDetails(ep.Handler.Method);
                var args = new object[parameters.Length];

                var (isOverridden, BindAsync) = IsBindAsyncOverridden(ep.GetType());

                if (isOverridden)
                {
                    _logger.LogDebug("Executing custom BindAsync method for endpoint");
                    var boundArgs = await (ValueTask<object[]>)BindAsync.Invoke(ep, [request, cancellationToken])!;

                    if (boundArgs.Length != parameters.Length)
                    {
                        _logger.LogWarning("Parameter length mismatch: expected {Expected} parameters, but BindAsync returned {Actual}. Filling missing parameters with default values.", parameters.Length, boundArgs.Length);

                        var argsWithDefaults = new object[parameters.Length];
                        Array.Copy(boundArgs, argsWithDefaults, boundArgs.Length);

                        for (int i = boundArgs.Length; i < parameters.Length; i++)
                            argsWithDefaults[i] = await BindParameter(parameters[i], sp, request, cancellationToken);

                        args = argsWithDefaults;
                    }
                    else
                    {
                        args = boundArgs;
                    }
                }
                else
                {
                    for (int i = 0; i < parameters.Length; i++)
                        args[i] = await BindParameter(parameters[i], sp, request, cancellationToken);
                }

                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogDebug("Endpoint argumment binding completed, invoking handler method");
                }

                // Invoke the delegate with the dynamically bound parameters
                if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
                {
                    bool isGenericTask = methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>);

                    var task = (Task)methodInfo.Invoke(ep.Handler.Target, args)!;
                    await task.ConfigureAwait(false);
                    result = isGenericTask ? ((dynamic)task).Result : null;
                }
                else
                {
                    // Invoke the delegate synchronously
                    result = methodInfo.Invoke(ep.Handler.Target, args)!;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("handler method execution completed");
                }
            }
            catch (TargetInvocationException ex)
            {
                _logger.LogError(ex, "Error occurred during method invocation.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing the request.");
                throw;
            }

            return result;
        };
    }

    public Task<object?> HandleAsync(IEndpoint endpoint, IServiceProvider sp, ILoggerFactory loggerFactory, HttpRequest request, CancellationToken cancellationToken)
    {
        return _handler(endpoint, sp, loggerFactory, request, cancellationToken);
    }

    private async ValueTask<object?> BindParameter(ParameterInfo param, IServiceProvider sp, HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (param.ParameterType == typeof(IFormFile) || param.ParameterType == typeof(IFormFileCollection))
            {
                var files = request.ReadFormFiles(param.Name!);
                return files?.Count == 1 ? files[0] : files;
            }

            if (param.ParameterType == typeof(HttpContext)) return request.HttpContext;
            if (param.ParameterType == typeof(HttpRequest)) return request;
            if (param.ParameterType == typeof(CancellationToken)) return cancellationToken;

            if (param.GetCustomAttribute<FromServicesAttribute>() != null)
            {
                return sp.GetRequiredService(param.ParameterType);
            }

            if (param.GetCustomAttribute<FromRouteAttribute>() is { } fromRouteAttribute)
            {
                if (request.RouteValues.TryGetValue(fromRouteAttribute.Name ?? param.Name!, out var routeValue) && routeValue is string routeString)
                {
                    return ConvertParameter(routeString, param.ParameterType);
                }
            }

            if (param.GetCustomAttribute<FromQueryAttribute>() is { } fromQueryAttribute)
            {
                if (request.Query.TryGetValue(fromQueryAttribute.Name ?? param.Name!, out var queryValue) && queryValue.Count > 0)
                {
                    return ConvertParameter(queryValue[0]!, param.ParameterType);
                }
            }

            if (param.GetCustomAttribute<FromHeaderAttribute>() is { } fromHeaderAttribute)
            {
                if (request.Headers.TryGetValue(fromHeaderAttribute.Name ?? param.Name!, out var headerValue) && headerValue.Count > 0)
                {
                    return ConvertParameter(headerValue[0]!, param.ParameterType);
                }
            }

            if (request.RouteValues.TryGetValue(param.Name!, out var routeVal) && routeVal is string routeStr && routeStr != $"{{{param.Name!}}}")
            {
                return ConvertParameter(routeStr, param.ParameterType);
            }

            if (request.Query.TryGetValue(param.Name!, out var queryStringValue) && queryStringValue.Count > 0)
            {
                return ConvertParameter(queryStringValue[0]!, param.ParameterType);
            }

            if (request.Headers.TryGetValue(param.Name!, out var headerStringValue) && headerStringValue.Count > 0)
            {
                return ConvertParameter(headerStringValue[0]!, param.ParameterType);
            }

            // Handle value type defaults
            if (param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("The value for parameter '{ParameterName}' was not found in the request, attempting to add default value.", param.Name);

                return param.HasDefaultValue ? param.DefaultValue : Activator.CreateInstance(param.ParameterType);
            }

            // Handle reference types or nullable value types
            if (request.ContentLength > 0 && !string.IsNullOrEmpty(request.ContentType))
            {
                if (request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    return await request.ReadFromJsonAsync(param.ParameterType, cancellationToken: cancellationToken);
                }
                
                if (request.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
                {
                    return await request.ReadFromXmlAsync(param.ParameterType, cancellationToken: cancellationToken);
                }
            }
          
            _logger.LogDebug("No value found for parameter '{ParameterName}'", param.Name);
            return param.HasDefaultValue ? param.DefaultValue : null;
        }
        catch (Exception pex)
        {
            _logger.LogError(pex, "An error occured while trying to bind value for parameter: {ParameterName}", param.Name);
            return null;
        }
    }

    private object? ConvertParameter(string value, Type type)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Converting type for value: '{ParameterValue}' to '{ParameterType}'", value, type.Name);
        }

        if (type == typeof(string)) return value;

        var underlyingType = Nullable.GetUnderlyingType(type);

        if (string.IsNullOrEmpty(value))
        {
            if (underlyingType != null) return null;
            if (!type.IsValueType) return null;
            return _valueTypeInstances.GetOrAdd(type, Activator.CreateInstance);
        }

        var targetType = underlyingType ?? type;

        if (targetType == typeof(int) && int.TryParse(value, out var intValue)) return intValue;
        if (targetType == typeof(double) && double.TryParse(value, out var doubleValue)) return doubleValue;
        if (targetType == typeof(decimal) && decimal.TryParse(value, out var decimalValue)) return decimalValue;
        if (targetType == typeof(bool) && bool.TryParse(value, out var boolValue)) return boolValue;
        if (targetType == typeof(long) && long.TryParse(value, out var longValue)) return longValue;
        if (targetType == typeof(float) && float.TryParse(value, out var floatValue)) return floatValue;
        if (targetType == typeof(byte) && byte.TryParse(value, out var byteValue)) return byteValue;
        if (targetType == typeof(short) && short.TryParse(value, out var shortValue)) return shortValue;
        if (targetType == typeof(sbyte) && sbyte.TryParse(value, out var sbyteValue)) return sbyteValue;
        if (targetType == typeof(ushort) && ushort.TryParse(value, out var ushortValue)) return ushortValue;
        if (targetType == typeof(uint) && uint.TryParse(value, out var uintValue)) return uintValue;
        if (targetType == typeof(ulong) && ulong.TryParse(value, out var ulongValue)) return ulongValue;
        if (targetType == typeof(char) && char.TryParse(value, out var charValue)) return charValue;
        if (targetType == typeof(TimeSpan) && TimeSpan.TryParse(value, out var timeSpanValue)) return timeSpanValue;
        if (targetType == typeof(DateTime) && DateTime.TryParse(value, out var dateTimeValue)) return dateTimeValue;
        if (targetType == typeof(Guid) && Guid.TryParse(value, out var guidValue)) return guidValue;
        if (targetType == typeof(DateOnly) && DateTime.TryParse(value, out var dateOnlyValue))
            return DateOnly.FromDateTime(dateOnlyValue);
        if (targetType == typeof(TimeOnly) && DateTime.TryParse(value, out var timeOnlyValue))
            return TimeOnly.FromDateTime(timeOnlyValue);
        if (targetType.IsEnum && Enum.TryParse(targetType, value, true, out var enumValue))
            return enumValue;

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return type.IsValueType && underlyingType == null
                ? _valueTypeInstances.GetOrAdd(type, Activator.CreateInstance)
                : null;
        }
    }

    private MethodDetails GetMethodDetails(MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo), "MethodInfo cannot be null");
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Getting method details for request endpoint");
        }

        return _methodCache.GetOrAdd(methodInfo, mi =>
        {
            var parameters = mi.GetParameters();
            return new MethodDetails
            {
                MethodInfo = mi,
                Parameters = parameters
            };
        });
    }

    public (bool, MethodInfo) IsBindAsyncOverridden(Type endpointType)
    {
        return _bindingCache.GetOrAdd(endpointType, type =>
        {
            var method = type.GetMethod(nameof(IEndpoint.BindAsync), [typeof(HttpRequest), typeof(CancellationToken)]);
            return (method != null && method.DeclaringType != typeof(IEndpoint), method);
        })!;
    }
}

public class MethodDetails
{
    public MethodInfo MethodInfo { get; set; }
    public ParameterInfo[] Parameters { get; set; } = Array.Empty<ParameterInfo>();

    public void Deconstruct(out MethodInfo methodInfo, out ParameterInfo[] parameters)
    {
        methodInfo = MethodInfo;
        parameters = Parameters;
    }
}