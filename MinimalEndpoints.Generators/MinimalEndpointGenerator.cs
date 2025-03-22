using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Immutable;

namespace MinimalEndpoints.Generators;

[Generator]
public class MinimalEndpointGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidateClasses = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.Members.Count > 0,
            transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
            ).Where(static candidate => candidate != null);

        var compilationAndCandidates = context.CompilationProvider.Combine(candidateClasses.Collect());

        context.RegisterSourceOutput(compilationAndCandidates, Execute);
    }

    private static void Execute(SourceProductionContext spc, (Compilation compilation, ImmutableArray<ClassDeclarationSyntax> candidates) source)
    {
        var desc = new DiagnosticDescriptor(
            id: "MINEND001",
            title: "Source Generator Message",
            messageFormat: "Found {0} endpoints.",
            category: "MinimalEndpointsGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
        spc.ReportDiagnostic(Diagnostic.Create(desc, Location.None, 0));

        var (compilation, candidates) = source;
        var endpoints = new List<EndpointInfo>();

        // Report total candidates found
        var candidatesDesc = new DiagnosticDescriptor(
            id: "MINEND002",
            title: "Candidate Classes",
            messageFormat: "Found {0} candidate classes.",
            category: "MinimalEndpointsGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
        spc.ReportDiagnostic(Diagnostic.Create(candidatesDesc, Location.None, candidates.Length));

        // Add a log message to indicate we're starting to process candidates
        var startProcessingDesc = new DiagnosticDescriptor(
            id: "MINEND003",
            title: "Processing Candidates",
            messageFormat: "Starting to process {0} candidate classes.",
            category: "MinimalEndpointsGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
        spc.ReportDiagnostic(Diagnostic.Create(startProcessingDesc, Location.None, candidates.Length));

        foreach (var candidate in candidates)
        {
            var model = compilation.GetSemanticModel(candidate.SyntaxTree);
            if (!(model.GetDeclaredSymbol(candidate) is INamedTypeSymbol typeSymbol))
                continue;

            // Only consider types that implement IEndpoint.
            if (!ImplementsIEndpoint(typeSymbol))
            {
                var notEndpointDesc = new DiagnosticDescriptor(
                    id: "MINEND003",
                    title: "Not Endpoint Class",
                    messageFormat: "Class {0} does not implement IEndpoint.",
                    category: "MinimalEndpointsGenerator",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true);
                spc.ReportDiagnostic(Diagnostic.Create(notEndpointDesc, Location.None, typeSymbol.Name));
                continue;
            }

            var interfaceType = "IEndpoint";
            if (typeSymbol.AllInterfaces.Any(i => i.Name == "IEndpointDefinition"))
            {
                interfaceType = "IEndpointDefinition";
            }

            // Look for an instance method decorated with [HandlerMethod]
            IMethodSymbol? handlerMethod = GetHandlerMethod(typeSymbol);

            if (handlerMethod == null && interfaceType != "IEndpointDefinition")
            {
                // Try to find the handler method from the Handler property delegate
                handlerMethod = GetHandlerMethodFromDelegate(typeSymbol);

                // If still no handler method found, report and skip
                if (handlerMethod == null)
                {
                    var noHandlerDesc = new DiagnosticDescriptor(
                        id: "MINEND004",
                        title: "No Handler Method",
                        messageFormat: "Class {0} does not have a method decorated with [HandlerMethod] or assigned to the Handler property.",
                        category: "MinimalEndpointsGenerator",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true);
                    spc.ReportDiagnostic(Diagnostic.Create(noHandlerDesc, Location.None, typeSymbol.Name));
                    continue;
                }
            }

            // Determine if BindAsync is overridden (i.e. declared on the type itself).
            bool isBindAsyncOverridden = typeSymbol.GetMembers("BindAsync")
                .OfType<IMethodSymbol>()
                .Any(m => !m.IsAbstract && SymbolEqualityComparer.Default.Equals(m.ContainingType, typeSymbol));

            var endpointInfo = new EndpointInfo
            {
                EndpointType = typeSymbol,
                HandlerMethod = handlerMethod!,
                IsBindAsyncOverridden = isBindAsyncOverridden,
                EndpointInterface = interfaceType,
                InheritsFromEndpointBase = InheritsFromType(typeSymbol, "EndpointBase")
            };

            // Look for the [Endpoint] attribute to retrieve extra metadata.
            var endpointAttr = typeSymbol.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "EndpointAttribute");
            if (endpointAttr != null)
            {
                foreach (var arg in endpointAttr.NamedArguments)
                {
                    if (arg.Key == "RouteName" && arg.Value.Value is string routeName)
                        endpointInfo.RouteName = routeName;
                    else if (arg.Key == "Description" && arg.Value.Value is string description)
                        endpointInfo.Description = description;
                    else if (arg.Key == "TagName" && arg.Value.Value is string tagName)
                        endpointInfo.TagName = tagName;
                    else if (arg.Key == "OperationId" && arg.Value.Value is string operationId)
                        endpointInfo.OperationId = operationId;
                    else if (arg.Key == "GroupName" && arg.Value.Value is string groupName)
                        endpointInfo.GroupName = groupName;
                    else if (arg.Key == "RoutePrefixOverride" && arg.Value.Value is string routePrefixOverride)
                        endpointInfo.RoutePrefixOverride = routePrefixOverride;
                    else if (arg.Key == "RateLimitingPolicyName" && arg.Value.Value is string rateLimitingPolicyName)
                        endpointInfo.RateLimitingPolicyName = rateLimitingPolicyName;
                    else if (arg.Key == "ExcludeFromDescription" && arg.Value.Value is bool excludeFromDescription)
                        endpointInfo.ExcludeFromDescription = excludeFromDescription;
                    else if (arg.Key == "DisableRateLimiting" && arg.Value.Value is bool disableRateLimiting)
                        endpointInfo.DisableRateLimiting = disableRateLimiting;
                }
            }

            endpoints.Add(endpointInfo);
        }

        // After endpoints are populated, report results and generate code
        var endpointsFoundDesc = new DiagnosticDescriptor(
            id: "MINEND005",
            title: "Endpoints Found",
            messageFormat: "Found {0} valid endpoints after processing. Will generate extension methods.",
            category: "MinimalEndpointsGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
        spc.ReportDiagnostic(Diagnostic.Create(endpointsFoundDesc, Location.None, endpoints.Count));

        // Generate the extension methods regardless of whether we found endpoints
        var generatedSource = GenerateExtensions(endpoints);
        spc.AddSource("MinimalEndpoints.g.cs", SourceText.From(generatedSource, Encoding.UTF8));
    }

    private static bool ImplementsIEndpoint(INamedTypeSymbol symbol)
    {
        return symbol.AllInterfaces.Any(i => i.Name == "IEndpoint" || i.Name == "IEndpointDefinition");
    }

    private static string GenerateExtensions(List<EndpointInfo> endpoints)
    {
        var sb = new StringBuilder();

        // File header and required using directives.
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
        sb.AppendLine("using Microsoft.AspNetCore.Builder;");
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using MinimalEndpoints.Authorization;");
        sb.AppendLine("using MinimalEndpoints.Extensions.Http.ContentNegotiation;");
        sb.AppendLine("using MinimalEndpoints.Extensions.Http.ModelBinding;");
        sb.AppendLine("using MinimalEndpoints.Extensions;");
        sb.NewLine();

        sb.AppendLine("namespace MinimalEndpoints;");
        sb.AppendLine("public static class MinimalEndpointExtensions");
        sb.AppendLine("{");

        #region AddGeneratedMinimalEndpoints
        // Generate DI registration.
        sb.AppendLineWithTab(1, "public static IServiceCollection AddGeneratedMinimalEndpoints(this IServiceCollection services)");
        sb.AppendLineWithTab(1, "{");

        // Original code for registering found endpoints
        foreach (var endpoint in endpoints)
        {
            var typeName = endpoint.EndpointType.ToDisplayString();
            sb.AppendFormattedLineWithTab(2, "services.AddScoped<{0}>();", typeName)
              .AppendFormattedLineWithTab(2, "services.AddScoped<{0},{1}>();", endpoint.EndpointInterface, typeName);
        }

        sb.NewLine();
        sb.AppendLineWithTab(2, "var descriptions = new EndpointDescriptors();");
        sb.AppendLineWithTab(2, "services.AddSingleton(sp =>")
          .AppendLineWithTab(2, "{")
          .AppendLineWithTab(3, "descriptions.___SetServiceProvicer___(sp);")
          .AppendLineWithTab(3, "return descriptions;")
          .AppendLineWithTab(2, "});");
        sb.NewLine();
        sb.AppendLineWithTab(2, "services.AddSingleton<IAuthorizationMiddlewareResultHandler, EndpointAuthorizationMiddlewareResultHandler>();");
        sb.AppendLineWithTab(2, "services.AddTransient<IResponseNegotiator, JsonResponseNegotiator>();");
        sb.AppendLineWithTab(2, "services.AddTransient<IResponseNegotiator, XmlResponseNegotiator>();");
        sb.AppendLineWithTab(2, "services.AddTransient<IEndpointModelBinder, JsonEndpointModelBiner>();");
        sb.AppendLineWithTab(2, "services.AddTransient<IEndpointModelBinder, XmlEndpointModelBinder>();");
        sb.NewLine();
        sb.AppendLineWithTab(2, "return services;");
        sb.AppendLineWithTab(1, "}");
        #endregion

        sb.NewLine();
        sb.NewLine();

        // Generate the endpoint mapping method.
        sb.AppendLineWithTab(1, "public static IEndpointRouteBuilder MapGeneratedMinimalEndpoints(this IEndpointRouteBuilder builder, Action<EndpointConfiguration>? configuration)");
        sb.AppendLineWithTab(1, "{");
        sb.AppendLineWithTab(2, "using var scope = builder.ServiceProvider.CreateScope();");
        sb.AppendLineWithTab(2, "var services = scope.ServiceProvider;")
          .NewLine();
        sb.AppendLineWithTab(2, "var endpointDescriptors = builder.ServiceProvider.GetRequiredService<EndpointDescriptors>();");
        sb.AppendLineWithTab(2, "var serviceConfig = new EndpointConfiguration")
          .AppendLineWithTab(2, "{")
          .AppendLineWithTab(3, "ServiceProvider = builder.ServiceProvider")
          .AppendLineWithTab(2, "};");
        sb.AppendLineWithTab(2, "configuration?.Invoke(serviceConfig);");
        sb.AppendLineWithTab(2, "var globalProduces = serviceConfig.Filters.OfType<ProducesResponseTypeAttribute>();");

        sb.NewLine();

        // Original code for mapping found endpoints
        for (int i = 0; i < endpoints.Count; i++)
        {
            EndpointInfo endpoint = endpoints[i];
            var typeName = endpoint.EndpointType.ToDisplayString();
            var name = endpoint.EndpointType.Name;

            sb.AppendFormattedLineWithTab(2, "#region Mapping for {0}", typeName);

            if (endpoint.EndpointInterface == "IEndpointDefinition")
            {

                sb.AppendLineWithTab(2, "#endregion")
                  .NewLine();
                continue;
            }

            #region IEndpoint Registration
            sb.AppendFormattedLineWithTab(2, "var name_{0} = \"{1}\";", i, name);

            // Use custom route from the [Endpoint] attribute if provided; otherwise, use the instance property.
            sb.AppendFormattedLineWithTab(2, "var temp_{0} = services.GetRequiredService<{1}>();", i, typeName);
            sb.AppendFormattedLineWithTab(2, "var routeName_{0} = \"{1}\";", i, (endpoint.RouteName ?? string.Empty));
            sb.AppendFormattedLineWithTab(2, "var pattern_{0} = temp_{0}.Pattern;", i)
              .NewLine();

            if (!string.IsNullOrWhiteSpace(endpoint.RoutePrefixOverride))
            {
                sb.AppendFormattedLineWithTab(2, "if (!pattern_{0}.StartsWith('~'))", i)
                  .AppendLineWithTab(2, "{")
                  .AppendFormattedLineWithTab(3, "pattern_{0} = $\"{1}/{{temp_{0}.Pattern.TrimStart('/')}}\";", i, endpoint.RoutePrefixOverride!.TrimEnd('/'))
                  .AppendLineWithTab(2, "}")
                  .NewLine();
            }

            var isAsync = (endpoint.IsBindAsyncOverridden || IsTaskLike(endpoint.HandlerMethod.ReturnType));

            sb.AppendFormattedLineWithTab(2, "var methods_{0} = new[] {{ temp_{0}.Method.Method }};", i);
            sb.NewLine();

            sb.AppendFormattedLineWithTab(2, "var handlerMethod_{0} = \"{1}\";", i, endpoint.HandlerMethod.Name);

            // Build the handler method name string directly
            var parameters = endpoint.HandlerMethod.Parameters;
            var formattedParameters = new List<string>();

            foreach (var param in parameters)
            {
                var paramType = param.Type;
                formattedParameters.Add(GetFullTypeNameWithBraces(paramType));
            }

            // Create the handler method name with properly formatted parameters
            var fullTypeName = endpoint.EndpointType.ToDisplayString().Replace("+", ".");
            var handlerMethodName = $"{fullTypeName}.{endpoint.HandlerMethod.Name}({string.Join(",", formattedParameters)})";

            sb.AppendFormattedLineWithTab(2, "var handlerMethodName_{0} = \"{1}\";", i, handlerMethodName);

            sb.AppendFormattedLineWithTab(2, "endpointDescriptors.Add(new EndpointDescriptor(");
            sb.AppendFormattedLineWithTab(3, "name: name_{0},", i);
            sb.AppendFormattedLineWithTab(3, "className: \"{0}\",", typeName);
            sb.AppendFormattedLineWithTab(3, "pattern: pattern_{0},", i);
            sb.AppendFormattedLineWithTab(3, "httpMethod: methods_{0}[0],", i);
            sb.AppendFormattedLineWithTab(3, "handlerMethod: handlerMethod_{0},", i);
            sb.AppendFormattedLineWithTab(3, "handlerIdentifier: handlerMethodName_{0},", i);
            sb.AppendFormattedLineWithTab(3, "routeName: routeName_{0}));", i);
            sb.NewLine();

            sb.AppendFormattedLineWithTab(2, "var mapping_{0} = builder.MapMethods(pattern_{0}, methods_{0}, {1} ([FromServices] IServiceProvider sp, [FromServices] ILoggerFactory loggerFactory, HttpRequest request, CancellationToken cancellationToken = default) =>",
                    i, isAsync ? "async" : "");
            sb.AppendLineWithTab(2, "{");
            sb.AppendFormattedLineWithTab(3, "var endpoint = sp.GetRequiredService<{0}>();", typeName);
            sb.AppendLineWithTab(3, "var context = request.HttpContext;");

            // Generate call to handler.
            var handlerParams = endpoint.HandlerMethod.Parameters;
            var callArgs = new List<string>();

            // Assume first parameter is HttpContext.
            if (endpoint.IsBindAsyncOverridden)
            {
                sb.AppendLineWithTab(3, "object[] boundParams = await endpoint.BindAsync(request);");
                int index = 0;
                for (int n = 0; n < handlerParams.Length; n++)
                {
                    var paramType = handlerParams[n].Type.ToDisplayString();
                    callArgs.Add($"({paramType})boundParams[{index}]");
                    index++;
                }
            }
            else
            {
                // Use individual ParameterBinder calls.
                for (int n = 0; n < handlerParams.Length; n++)
                {
                    var param = handlerParams[n];
                    if (handlerParams[n].Type.Name == "HttpRequest")
                    {
                        callArgs.Add("request");
                        continue;
                    }
                    if (handlerParams[n].Type.Name == "HttpContext")
                    {
                        callArgs.Add("context");
                        continue;
                    }
                    if (handlerParams[n].Type.Name == "CancellationToken")
                    {
                        callArgs.Add("cancellationToken");
                        continue;
                    }
                    callArgs.Add(GetBinderCall(param, isAsync));
                }
            }

            sb.AppendLine(GenerateHandlerCall(endpoint, callArgs, isAsync))
              .NewLine();

            sb.AppendLineWithTab(3, "return result;");
            sb.AppendLineWithTab(2, "});");
            sb.NewLine();

            if (endpoint.ExcludeFromDescription)
            {
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.ExcludeFromDescription();", i);
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Description))
            {
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithDescription(\"{1}\");", i, endpoint.Description!);
            }

            if (!string.IsNullOrWhiteSpace(endpoint.OperationId))
            {
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithName(\"{1}\");", i, endpoint.OperationId!);
            }

            if (!string.IsNullOrWhiteSpace(endpoint.TagName))
            {
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithTags(\"{1}\");", i, endpoint.TagName!);
            }

            if (!string.IsNullOrWhiteSpace(endpoint.GroupName))
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithGroupName(\"{1}\");", i, endpoint.GroupName!);
            else
            {
                sb.AppendLineWithTab(2, "if (serviceConfig.DefaultGroupName is { })")
                  .AppendFormattedLineWithTab(3, "mapping_{0}.WithGroupName(serviceConfig.DefaultGroupName);", i);
            }

            sb.NewLine();

            sb.AppendLineWithTab(2, "if (!string.IsNullOrWhiteSpace(serviceConfig.DefaultRateLimitingPolicyName))")
              .AppendFormattedLineWithTab(3, "mapping_{0}.RequireRateLimiting(serviceConfig.DefaultRateLimitingPolicyName);", i);

            sb.NewLine();

            if (endpoint.RateLimitingPolicyName is { } || endpoint.DisableRateLimiting is { })
            {
                if (!string.IsNullOrWhiteSpace(endpoint.RateLimitingPolicyName))
                    sb.AppendFormattedLineWithTab(2, "mapping_{0}.RequireRateLimiting(\"{1}\");", i, endpoint.RateLimitingPolicyName!);
                else if (endpoint.DisableRateLimiting)
                    sb.AppendFormattedLineWithTab(2, "mapping_{0}.DisableRateLimiting();", i);
            }

            if (!string.IsNullOrWhiteSpace(endpoint.RouteName))
            {
                sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithMetadata(new EndpointNameMetadata(\"{1}\"));", i, endpoint.RouteName!)
                  .NewLine();
            }

            sb.AppendFormattedLineWithTab(2, "mapping_{0}.WithMetadata(new HttpMethodMetadata(methods_{0}))", i)
              .AppendFormattedLineWithTab(3, ".WithDisplayName(name_{0});", i)
              .NewLine();

            // Get ProducesResponseTypeAttribute from endpoint class
            sb.AppendFormattedLineWithTab(2, "var producesRespAttributes_{0} = new List<ProducesResponseTypeAttribute>();", i);

            // Add attributes from the endpoint class
            var producesAttrs = endpoint.EndpointType.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ProducesResponseTypeAttribute")
                .ToList();

            if (producesAttrs.Any())
            {
                foreach (var attr in producesAttrs)
                {
                    // Extract status code from attribute constructor
                    var statusCode = attr.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? "200";

                    // Extract type from attribute constructor (if available)
                    var typeArg = attr.ConstructorArguments.Count() > 1
                        ? attr.ConstructorArguments[1].Value as ITypeSymbol
                        : null;

                    string ptypeName = "typeof(void)";
                    bool isVoidType = false;

                    if (typeArg != null)
                    {
                        if (typeArg.Name == "Void" && typeArg.ContainingNamespace?.Name == "System")
                        {
                            isVoidType = true;
                            ptypeName = "typeof(void)";
                        }
                        else
                        {
                            ptypeName = $"typeof({typeArg.ToDisplayString()})";
                        }
                    }

                    // Generate code to create the attribute
                    sb.AppendFormattedLineWithTab(2, "producesRespAttributes_{0}.Add(new ProducesResponseTypeAttribute({1}, {2}));",
                        i, ptypeName, statusCode);
                }
                sb.NewLine();
            }

            // Add global produces attributes
            sb.AppendFormattedLineWithTab(2, "if (globalProduces.Any()) producesRespAttributes_{0}.AddRange(globalProduces);", i);
            sb.NewLine();

            // Process each attribute
            sb.AppendFormattedLineWithTab(2, "foreach (var attr in producesRespAttributes_{0})", i);
            sb.AppendLineWithTab(2, "{");
            sb.AppendLineWithTab(3, "if (attr.Type == typeof(void))");
            sb.AppendLineWithTab(3, "{");
            sb.AppendLineWithTab(4, "if (attr.StatusCode == StatusCodes.Status400BadRequest)");
            sb.AppendLineWithTab(4, "{");
            sb.AppendFormattedLineWithTab(5, "mapping_{0}.ProducesValidationProblem();", i);
            sb.AppendLineWithTab(5, "continue;");
            sb.AppendLineWithTab(4, "}");
            sb.NewLine();
            sb.AppendLineWithTab(4, "if (attr.StatusCode == StatusCodes.Status500InternalServerError)");
            sb.AppendLineWithTab(4, "{");
            sb.AppendFormattedLineWithTab(5, "mapping_{0}.ProducesProblem(attr.StatusCode);", i);
            sb.AppendLineWithTab(5, "continue;");
            sb.AppendLineWithTab(4, "}");
            sb.NewLine();
            sb.AppendFormattedLineWithTab(4, "mapping_{0}.Produces(attr.StatusCode, responseType: null);", i);
            sb.AppendLineWithTab(4, "continue;");
            sb.AppendLineWithTab(3, "}")
              .NewLine();

            sb.AppendFormattedLineWithTab(3, "mapping_{0}.Produces(attr.StatusCode, responseType: attr.Type);", i);
            sb.AppendFormattedLineWithTab(3, "mapping_{0}.WithMetadata(new ProducesResponseTypeMetadata(attr.StatusCode, attr.Type));", i);
            sb.AppendLineWithTab(2, "}")
              .NewLine();

            sb.AppendLineWithTab(2, "foreach (var filter in serviceConfig.EndpointFilters)")
              .AppendLineWithTab(2, "{")
              .AppendFormattedLineWithTab(3, "mapping_{0}.AddEndpointFilter(filter);", i)
              .AppendLineWithTab(2, "}")
              .NewLine();

            if (endpoint.InheritsFromEndpointBase)
            {
                sb.AppendFormattedLineWithTab(2, "var ep_{0} = (EndpointBase)temp_{0};", i)
                  .AppendFormattedLineWithTab(2, "foreach (var filter in ep_{0}.EndpointFilters)", i)
                  .AppendLineWithTab(2, "{")
                  .AppendFormattedLineWithTab(3, "mapping_{0}.AddEndpointFilter(filter);", i)
                  .AppendLineWithTab(2, "}")
                  .NewLine();
            }
            #endregion

            sb.AppendLineWithTab(2, "#endregion")
              .NewLine();
        }

        sb.NewLine();
        sb.AppendLineWithTab(2, "return builder;")
          .AppendLineWithTab(1, "}");

        sb.AppendLine("}");

        return sb.ToString();
    }

    #region Helper Methods
    private static string GetBinderCall(IParameterSymbol parameter, bool useAsync)
    {
        string paramName = parameter.Name;
        string typeName = parameter.Type.ToDisplayString();
        string defaultValuePart = HasDefaultValue(parameter)
            ? $", defaultValue: {GetDefaultValueString(parameter)}"
            : "";

        if (parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "FromRouteAttribute"))
            return $"ParameterBinder.BindFromRoute<{typeName}>(context, \"{paramName}\"{defaultValuePart})";
        else if (parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "FromQueryAttribute"))
            return $"ParameterBinder.BindFromQuery<{typeName}>(context, \"{paramName}\"{defaultValuePart})";
        else if (parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "FromBodyAttribute"))
            return $"await ParameterBinder.BindFromBodyAsync<{typeName}>(context)";
        else if (parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "FromHeaderAttribute"))
            return $"ParameterBinder.BindFromHeader<{typeName}>(context, \"{paramName}\"{defaultValuePart})";
        else if (parameter.GetAttributes().Any(a => a.AttributeClass?.Name == "FromServicesAttribute"))
            return $"ParameterBinder.BindFromServices<{typeName}>(context)";
        else
        {
            // No attribute: default heuristics.
            if (parameter.Type.IsValueType || parameter.Type.SpecialType == SpecialType.System_String)
                return useAsync ? $"await ParameterBinder.BindDefaultAsync<{typeName}>(context, \"{paramName}\"{defaultValuePart})"
                    : $"ParameterBinder.BindDefaultAsync<{typeName}>(context, \"{paramName}\"{defaultValuePart}).Result";
            else
                return useAsync ? $"await ParameterBinder.BindFromBodyAsync<{typeName}>(context)"
                    : $"ParameterBinder.BindFromBodyAsync<{typeName}>(context).Result";
        }
    }

    private static bool IsTaskLike(ITypeSymbol typeSymbol)
    {
        // Check if it's Task
        if (typeSymbol.MetadataName == "Task" &&
            typeSymbol.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
        {
            return true;
        }

        // Check if it's Task<T>
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.ConstructedFrom.MetadataName == "Task" &&
            namedType.ConstructedFrom.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks")
        {
            return true;
        }

        // Check if it's a type that inherits/implements Task (ValueTask, etc.)
        if (typeSymbol.AllInterfaces.Any(i =>
            i.MetadataName == "IAsyncResult" &&
            i.ContainingNamespace.ToDisplayString() == "System"))
        {
            return true;
        }

        return false;
    }

    // Add this helper method to check if a parameter has a default value
    private static bool HasDefaultValue(IParameterSymbol parameter)
    {
        return parameter.HasExplicitDefaultValue;
    }

    // Add this helper method to get the default value (with proper C# formatting)
    private static string GetDefaultValueString(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue)
            return string.Empty;

        var defaultValue = parameter.ExplicitDefaultValue;

        // Handle null
        if (defaultValue == null)
            return "null";

        // Handle string values (need quotes)
        if (defaultValue is string stringValue)
            return $"\"{stringValue}\"";

        // Handle char values (need single quotes)
        if (defaultValue is char charValue)
            return $"'{charValue}'";

        // Handle boolean values (lowercase in C#)
        if (defaultValue is bool boolValue)
            return boolValue ? "true" : "false";

        // Other primitive types and enums
        return defaultValue.ToString();
    }

    // Helper method to format generic types recursively
    private static string FormatGenericType(INamedTypeSymbol type)
    {
        var baseTypeName = type.ConstructedFrom.ToDisplayString(new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.None));

        // Remove the backtick part if present
        int tickIndex = baseTypeName.IndexOf('`');
        if (tickIndex > 0)
            baseTypeName = baseTypeName.Substring(0, tickIndex);

        // Format each generic argument recursively
        var argStrings = type.TypeArguments.Select(arg =>
            arg is INamedTypeSymbol argNamed && argNamed.IsGenericType
                ? FormatGenericType(argNamed)
                : arg.ToDisplayString().Replace("+", "."));

        return $"{baseTypeName}{{{string.Join(",", argStrings)}}}";
    }

    // Add this helper method to the class:
    private static string GetFullTypeNameWithBraces(ITypeSymbol type)
    {
        // Handle primitive types by getting their full name
        if (type.SpecialType != SpecialType.None)
        {
            // Map special types to their full names
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return "System.Boolean";
                case SpecialType.System_Byte: return "System.Byte";
                case SpecialType.System_SByte: return "System.SByte";
                case SpecialType.System_Int16: return "System.Int16";
                case SpecialType.System_UInt16: return "System.UInt16";
                case SpecialType.System_Int32: return "System.Int32";
                case SpecialType.System_UInt32: return "System.UInt32";
                case SpecialType.System_Int64: return "System.Int64";
                case SpecialType.System_UInt64: return "System.UInt64";
                case SpecialType.System_Decimal: return "System.Decimal";
                case SpecialType.System_Single: return "System.Single";
                case SpecialType.System_Double: return "System.Double";
                case SpecialType.System_Char: return "System.Char";
                case SpecialType.System_String: return "System.String";
                case SpecialType.System_Object: return "System.Object";
                case SpecialType.System_Void: return "System.Void";
                default: return type.ToDisplayString().Replace("+", ".");
            }
        }

        // Handle generic types
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var containerType = namedType.ContainingType;
            string containerPrefix = containerType != null ?
                GetFullTypeNameWithBraces(containerType) + "." : "";

            // Special handling for Nullable<T>
            if (namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
            {
                var typeArg = namedType.TypeArguments[0];
                return $"System.Nullable{{{GetFullTypeNameWithBraces(typeArg)}}}";
            }

            // Get base type name without generic parameters
            var baseTypeName = namedType.Name;
            int tickIndex = baseTypeName.IndexOf('`');
            if (tickIndex > 0)
                baseTypeName = baseTypeName.Substring(0, tickIndex);

            // Get namespace
            var ns = namedType.ContainingNamespace;
            var namespaceString = ns.IsGlobalNamespace ? "" : ns.ToDisplayString() + ".";

            // Format the generic arguments recursively
            var typeArgs = namedType.TypeArguments;
            var typeArgStrings = typeArgs.Select(GetFullTypeNameWithBraces);

            return $"{namespaceString}{containerPrefix}{baseTypeName}{{{string.Join(",", typeArgStrings)}}}";
        }

        // Handle arrays
        if (type is IArrayTypeSymbol arrayType)
        {
            return $"{GetFullTypeNameWithBraces(arrayType.ElementType)}[]";
        }

        // Handle regular, non-generic types
        return type.ToDisplayString().Replace("+", ".");
    }

    private static bool InheritsFromType(INamedTypeSymbol symbol, string baseTypeName)
    {
        var baseType = symbol.BaseType;

        while (baseType != null)
        {
            if (baseType.Name == baseTypeName)
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    private static IMethodSymbol? GetHandlerMethod(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol == null || typeSymbol.SpecialType == SpecialType.System_Object)
        {
            return null;
        }

        // First try to find a method with the attribute directly on this type
        var handlerMethod = typeSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m =>
                !m.IsStatic &&
                m.GetAttributes().Any(a => a.AttributeClass?.Name == "HandlerMethodAttribute"));

        if (handlerMethod != null)
        {
            return handlerMethod;
        }

        // If no direct method found, check base classes for a method with the attribute
        var baseHandlerMethod = GetHandlerMethod(typeSymbol.BaseType);
        if (baseHandlerMethod != null)
        {
            // If found in base class, look for an override in the current class
            var overriddenMethod = typeSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .FirstOrDefault(m => 
                    !m.IsStatic && 
                    m.Name == baseHandlerMethod.Name && 
                    m.Parameters.Length == baseHandlerMethod.Parameters.Length &&
                    m.IsOverride);
            
            // Return the override if found, otherwise return the base method
            return overriddenMethod ?? baseHandlerMethod;
        }

        return null;
    }

    private static IMethodSymbol? GetHandlerMethodFromDelegate(INamedTypeSymbol typeSymbol)
    {
        // Look for a property named "Handler" that returns Delegate
        var handlerProperty = typeSymbol.GetMembers("Handler")
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Type.Name == "Delegate" || p.Type.BaseType?.Name == "Delegate");

        if (handlerProperty == null)
            return null;

        // Find the syntax node for the property declaration
        var declarationSyntax = handlerProperty.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
        if (declarationSyntax == null)
            return null;

        // Look for an arrow expression body (e.g., "public Delegate Handler => DeleteAsync;")
        if (declarationSyntax.ExpressionBody != null)
        {
            var methodNameExpr = declarationSyntax.ExpressionBody.Expression as IdentifierNameSyntax;
            if (methodNameExpr != null)
            {
                string methodName = methodNameExpr.Identifier.ValueText;

                return typeSymbol.GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .FirstOrDefault(m => !m.IsStatic);
            }
        }

        // Look for a getter with a return statement (e.g., "{ get { return DeleteAsync; } }")
        if (declarationSyntax.AccessorList != null)
        {
            var getAccessor = declarationSyntax.AccessorList.Accessors
                .FirstOrDefault(a => a.Keyword.ValueText == "get");

            if (getAccessor?.Body != null)
            {
                var returnStatement = getAccessor.Body.Statements
                    .OfType<ReturnStatementSyntax>()
                    .FirstOrDefault();

                if (returnStatement?.Expression is IdentifierNameSyntax returnIdentifier)
                {
                    string methodName = returnIdentifier.Identifier.ValueText;

                    // Find the method with this name in the class
                    return typeSymbol.GetMembers(methodName)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(m => !m.IsStatic);
                }
            }
        }

        return null;
    }

    private static string GenerateHandlerCall(EndpointInfo endpoint, List<string> callArgs, bool isAsync)
    {
        var sb = new StringBuilder();
        var handlerMethod = endpoint.HandlerMethod;
        
        // Check if handler method is public
        if (handlerMethod.DeclaredAccessibility == Accessibility.Public)
        {
            // Use direct method call
            sb.AppendWithTab(3, "var result = ");
            if (isAsync)
                sb.Append("await ");
            sb.Append($"endpoint.{handlerMethod.Name}({string.Join(", ", callArgs)});");
        }
        else
        {
            // Use the Handler delegate property
            sb.AppendLineWithTab(3, "// Handler method is not public, using the Handler delegate property");
            sb.AppendLineWithTab(3, "var handlerDelegate = endpoint.Handler;");
            
            // Create the appropriate delegate type based on parameters and return type
            string delegateType = GetDelegateType(handlerMethod);
            
            sb.AppendLineWithTab(3, $"var typedHandler = ({delegateType})handlerDelegate;");
            
            // Call the delegate
            sb.AppendWithTab(3, "var result = ");
            if (isAsync)
                sb.Append("await ");
            sb.Append($"typedHandler({string.Join(", ", callArgs)});");
        }
        
        return sb.ToString();
    }

    private static string GetDelegateType(IMethodSymbol method)
    {
        var returnType = method.ReturnType.ToDisplayString();
        bool isTask = IsTaskLike(method.ReturnType);
        
        // For methods with Task<T> return type, extract T
        if (isTask && method.ReturnType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            returnType = namedType.TypeArguments[0].ToDisplayString();
        }
        else if (isTask) // Plain Task
        {
            returnType = "void";
        }
        
        // Build the delegate type based on parameters and return type
        var parameters = string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString()));
        
        if (returnType == "void")
        {
            if (parameters.Length == 0)
                return "System.Action";
            return $"System.Action<{parameters}>";
        }
        else
        {
            if (parameters.Length == 0)
                return $"System.Func<{returnType}>";
            return $"System.Func<{parameters}, {returnType}>";
        }
    }
    #endregion
}

internal class EndpointInfo
{
    public INamedTypeSymbol EndpointType { get; set; }
    public IMethodSymbol HandlerMethod { get; set; }
    public string EndpointInterface { get; set; } = "IEndpoint";
    public bool IsBindAsyncOverridden { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public string? TagName { get; set; } = string.Empty;
    public string? OperationId { get; set; } = null;
    public string? GroupName { get; set; } = null;
    public bool ExcludeFromDescription { get; set; } = false;
    public string? RoutePrefixOverride { get; set; } = null;
    public string? Description { get; set; } = null;
    public string? RateLimitingPolicyName { get; set; } = null!;
    public bool DisableRateLimiting { get; set; } = false;
    public bool InheritsFromEndpointBase { get; set; } = false;
}

