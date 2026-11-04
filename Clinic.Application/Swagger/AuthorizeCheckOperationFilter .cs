using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Clinic.Api.Swagger;

public sealed class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var method = context.MethodInfo;
        var controller = method.DeclaringType;

        var allowsAnonymous =
            method.GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .Any()
            || controller?
                .GetCustomAttributes(inherit: true)
                .OfType<AllowAnonymousAttribute>()
                .Any() == true;

        if (allowsAnonymous)
            return;

        var requiresAuthorization =
            method.GetCustomAttributes(inherit: true)
                .OfType<AuthorizeAttribute>()
                .Any()
            || controller?
                .GetCustomAttributes(inherit: true)
                .OfType<AuthorizeAttribute>()
                .Any() == true;

        if (!requiresAuthorization)
            return;

        operation.Security ??=
            new List<OpenApiSecurityRequirement>();

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    }
}
