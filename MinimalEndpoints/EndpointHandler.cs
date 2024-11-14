using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalEndpoints.Extensions;
using MinimalEndpoints.Extensions.Http;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalEndpoints;

public class EndpointHandler
{
    private readonly ILogger<EndpointHandler> _logger;
    private readonly Func<IEndpoint, IServiceProvider, ILoggerFactory, HttpRequest, CancellationToken, Task<object?>> _handler;

    public EndpointHandler(ILogger<EndpointHandler> logger)
    {
        _handler = CreateHandler();
        _logger = logger;

        _valueTypeInstances.GetOrAdd(typeof(int), Activator.CreateInstance);
        _valueTypeInstances.GetOrAdd(typeof(bool), Activator.CreateInstance);
        _valueTypeInstances.GetOrAdd(typeof(double), Activator.CreateInstance);
        _valueTypeInstances.GetOrAdd(typeof(float), Activator.CreateInstance);
    }

    private Func<IEndpoint, IServiceProvider, ILoggerFactory, HttpRequest, CancellationToken, Task<object?>> CreateHandler()
    {
        return async (endpoint, sp, loggerFactory, request, cancellationToken) =>
        {
            object? result = null!;
            using var _ = _logger.AddContext("EndpointName", endpoint.GetType().FullName);
            try
            {
                _logger.LogInformation("Executing endpoint: '{EndpointName}'", endpoint.GetType().FullName);
                var ep = (IEndpoint)sp.GetService(endpoint.GetType())!;

                var (methodInfo, parameters) = GetMethodDetails(ep.Handler.Method);
                var args = new object[parameters.Length];

                var (isOverridden, BindAsync) = IsBindAsyncOverridden(ep.GetType());

                if (isOverridden)
                {
                    _logger.LogDebug("Executing custom BindAsync method for endpoint");
                    var boundArgs = await (ValueTask<object[]>)BindAsync.Invoke(ep, new object[] { request, cancellationToken })!;

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

                _logger.LogDebug("Endpoint argumment binding completed, invoking handler method");

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

                _logger.LogDebug("handler method execution completed");
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


    private static readonly ConcurrentDictionary<Type, object?> _valueTypeInstances = new ConcurrentDictionary<Type, object?>();
    private object? ConvertParameter(string value, Type type)
    {
        _logger.LogDebug("Converting type for value: '{ParameterValue}' to '{ParameterType}'", value, type.Name);
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
    private async Task<object?> BindParameter(ParameterInfo param, IServiceProvider sp, HttpRequest request, CancellationToken cancellationToken)
    {
        object? value = null;
        object? requestBodyObject = null!;

        try
        {
            if (param.ParameterType == typeof(IFormFile) || param.ParameterType == typeof(IFormFileCollection))
            {
                var files = request.ReadFormFiles(param.Name!);
                if (files is { })
                    value = files is { Count: 1 } ? files[0] : (IFormFileCollection)files;
            }
            else if (param.ParameterType == typeof(HttpContext))
            {
                value = request.HttpContext;
            }
            else if (param.ParameterType == typeof(HttpRequest))
            {
                value = request;
            }
            else if (param.ParameterType == typeof(CancellationToken))
            {
                value = cancellationToken;
            }
            else if (param.GetCustomAttribute<FromServicesAttribute>() != null)
            {
                value = sp.GetRequiredService(param.ParameterType);
            }
            else if (param.GetCustomAttribute<FromRouteAttribute>() is { } fromRouteAttribute)
            {
                value = request.RouteValues[fromRouteAttribute.Name ?? param.Name!]?.ToString();
                if (value is { })
                    value = ConvertParameter(value.ToString()!, param.ParameterType);
            }
            else if (param.GetCustomAttribute<FromQueryAttribute>() is { } fromQueryAttribute)
            {
                value = request.Query[fromQueryAttribute.Name ?? param.Name!].FirstOrDefault();
                if (value is { })
                    value = ConvertParameter(value.ToString()!, param.ParameterType);
            }
            else if (param.GetCustomAttribute<FromHeaderAttribute>() is { } fromHeaderAttribute)
            {
                value = request.Headers[fromHeaderAttribute.Name ?? param.Name!].FirstOrDefault();
                if (value is { })
                    value = ConvertParameter(value.ToString()!, param.ParameterType);
            }
            else
            {
                var routeVal = request.RouteValues[param.Name!]?.ToString();
                if (routeVal == $"{{{param.Name!}}}") routeVal = null!;

                string stringValue = routeVal! ??
                                     request.Query[param.Name!].FirstOrDefault()! ??
                                     request.Headers[param.Name!].FirstOrDefault()!;

                if (!string.IsNullOrEmpty(stringValue))
                    value = ConvertParameter(stringValue, param.ParameterType);
            }

            if (value == null && param.ParameterType.IsValueType && Nullable.GetUnderlyingType(param.ParameterType) == null)
            {
                if (param.HasDefaultValue)
                {
                    value = param.DefaultValue!;
                }
                else
                {
                    _logger.LogDebug("The value for parameter '{ParameterName}' was not found in the request and does not have a default value.", param.Name);
                    throw new InvalidOperationException($"The value for parameter '{param.Name}' was not found in the request and does not have a default value.");
                }
            }

            if (value == null && (!param.ParameterType.IsValueType || Nullable.GetUnderlyingType(param.ParameterType) != null))
            {
                if (requestBodyObject == null && request is { ContentLength: > 0, ContentType.Length: > 0 })
                {
                    if (request.ContentType.Contains("json", StringComparison.OrdinalIgnoreCase))
                    {
                        requestBodyObject = await request.ReadFromJsonAsync(param.ParameterType, cancellationToken: cancellationToken);
                    }
                    else if (request.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
                    {
                        requestBodyObject = await request.ReadFromXmlAsync(param.ParameterType, cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    _logger.LogDebug("No data was found in the request body or content type '{ContentType}' is not supported", request.ContentType);
                }

                value = requestBodyObject ?? (param.HasDefaultValue ? param.DefaultValue : null);
            }
        }
        catch (Exception pex)
        {
            _logger.LogError(pex, "An error occured while trying to bind value for parameter: {ParameterName}", param.Name);
        }

        return value;
    }

    #region MethodDetailsCache
    private static readonly ConcurrentDictionary<MethodInfo, MethodDetails> _methodCache = new ConcurrentDictionary<MethodInfo, MethodDetails>();

    private MethodDetails GetMethodDetails(MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo), "MethodInfo cannot be null");
        }

        _logger.LogDebug("Getting method details for request endpoint");

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

    internal class MethodDetails
    {
        public MethodInfo MethodInfo { get; set; }
        public ParameterInfo[] Parameters { get; set; }

        public void Deconstruct(out MethodInfo methodInfo, out ParameterInfo[] parameters)
        {
            methodInfo = this.MethodInfo;
            parameters = this.Parameters;
        }
    }
    #endregion

    #region BindAsyncOverrideCache
    private static readonly ConcurrentDictionary<Type, (bool IsOverridden, MethodInfo? Method)> _bindingCache = new ConcurrentDictionary<Type, (bool, MethodInfo?)>();

    public (bool, MethodInfo) IsBindAsyncOverridden(Type endpointType)
    {
        return _bindingCache.GetOrAdd(endpointType, type =>
        {
            var method = type.GetMethod(nameof(IEndpoint.BindAsync), [typeof(HttpRequest), typeof(CancellationToken)]);
            return (method != null && method.DeclaringType != typeof(IEndpoint), method);
        })!;
    }
    #endregion
}

