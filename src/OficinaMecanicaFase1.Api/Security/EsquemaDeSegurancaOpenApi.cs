using Microsoft.OpenApi;

namespace OficinaMecanicaFase1.Api.Security;

public static class EsquemaDeSegurancaOpenApi
{
    public static Task AdicionarAsync(OpenApiDocument documento)
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            In = ParameterLocation.Header,
            BearerFormat = "JWT"
        };

        foreach (var (rota, caminho) in documento.Paths)
        {
            if (!rota.StartsWith("/api/interno/", StringComparison.OrdinalIgnoreCase) ||
                rota.Equals("/api/interno/autenticacao", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var operacao in caminho.Operations.Values)
            {
                operacao.Security ??= [];
                operacao.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", documento)] = []
                });
            }
        }

        return Task.CompletedTask;
    }
}
