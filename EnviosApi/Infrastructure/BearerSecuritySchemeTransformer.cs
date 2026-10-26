using System;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace EnviosApi.Infrastructure;

public class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string SchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Pegá el token JWT obtenido en POST /api/auth/login (sin el prefijo 'Bearer')."
        };

        // Requisito global: Swagger UI manda el token en todas las operaciones una vez autorizado.
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(SchemeName, document)] = new List<string>()
        });

        return Task.CompletedTask;
    }
}
