﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MinimalEndpoints.WebApiDemo;

public class SecureSwaggerEndpointhRequirementFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var isAuthorized = actionMetadata.Any(metadataItem => metadataItem is AuthorizeAttribute);
        var allowAnonymous = actionMetadata.Any(metadataItem => metadataItem is AllowAnonymousAttribute);

        if (!isAuthorized || allowAnonymous)
        {
            return;
        }
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        operation.Security = new List<OpenApiSecurityRequirement>
            {
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                }
            };
    }
}