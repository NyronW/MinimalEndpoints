using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalEndpoints.Extensions.Http;
using System.Collections.Concurrent;
using System.Reflection;

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

            try
            {
                var ep = (IEndpoint)sp.GetService(endpoint.GetType())!;

                var (methodInfo, parameters) = GetMethodDetails(ep.Handler.Method);
                var args = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param is not { Name.Length: > 0 }) continue;

                    object? value = null;
                    object? requestBodyObject = null!;

                    var (isOverridden, BindAsync) = IsBindAsyncOverridden(ep.GetType());

                    if (isOverridden)
                    {
                        value = await (ValueTask<object>)BindAsync.Invoke(ep, [request, cancellationToken])!;
                    }
                    else if (param.ParameterType == typeof(IFormFile) || param.ParameterType == typeof(IFormFileCollection))
                    {
                        var files = request.ReadFormFiles(param.Name);
                        if (files is { }) value = files is { Count: 1 } ? files[0] : (IFormFileCollection)files;
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
                    else
                    {
                        string stringValue = request.RouteValues[param.Name]?.ToString()! ??
                                             request.Query[param.Name].FirstOrDefault()! ??
                                             request.Headers[param.Name].FirstOrDefault()!;

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
                            _logger.LogDebug("No data was found the request body or content type '{ContentType}' is not supported", request.ContentType);

                        value = requestBodyObject ?? (param.HasDefaultValue ? param.DefaultValue : null);
                    }

                    args[i] = value!;
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
        if (type == typeof(string)) return value;

        if (string.IsNullOrEmpty(value))
        {
            if (!type.IsValueType) return null;
            return Nullable.GetUnderlyingType(type) != null
                ? null
                : _valueTypeInstances.GetOrAdd(type, Activator.CreateInstance);
        }

        if ((type == typeof(int) || type == typeof(int?)) && int.TryParse(value, out var intValue)) return intValue;
        if ((type == typeof(double) || type == typeof(double?)) && double.TryParse(value, out var doubleValue)) return doubleValue;
        if ((type == typeof(bool) || type == typeof(bool?)) && bool.TryParse(value, out var boolValue)) return boolValue;
        if ((type == typeof(DateTime) || type == typeof(DateTime?)) && DateTime.TryParse(value, out var dateTimeValue)) return dateTimeValue;
        if ((type == typeof(Guid) || type == typeof(Guid?)) && Guid.TryParse(value, out var guidValue)) return guidValue;
        if ((type == typeof(long) || type == typeof(long?)) && long.TryParse(value, out var longValue)) return longValue;
        if ((type == typeof(float) || type == typeof(float?)) && float.TryParse(value, out var floatValue)) return floatValue;

        try
        {
            return Convert.ChangeType(value, type);
        }
        catch (FormatException)
        {
            return type.IsValueType ? _valueTypeInstances.GetOrAdd(type, Activator.CreateInstance) : null;
        }
    }

    #region MethodDetailsCache
    private static readonly ConcurrentDictionary<MethodInfo, MethodDetails> _methodCache = new ConcurrentDictionary<MethodInfo, MethodDetails>();

    private MethodDetails GetMethodDetails(MethodInfo methodInfo)
    {
        if (methodInfo == null)
        {
            throw new ArgumentNullException(nameof(methodInfo), "MethodInfo cannot be null");
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

