using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using OficinaMecanicaFase1.Api.Security;
using OficinaMecanicaFase1.Application.AcessoInterno;
using OficinaMecanicaFase1.Application.Cadastros;
using OficinaMecanicaFase1.Application.Catalogo;
using OficinaMecanicaFase1.Application.OrdensServico;
using OficinaMecanicaFase1.Infrastructure.Identity;
using OficinaMecanicaFase1.Infrastructure.DependencyInjection;

namespace OficinaMecanicaFase1.Api.DependencyInjection;

// Ponto de composição: conecta os casos de uso às implementações externas.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOficinaMecanica(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        });
        services.AddControllers(options =>
                options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
            .ConfigureApiBehaviorOptions(options => options.SuppressMapClientErrors = true);
        services.AddOpenApi(options => options.AddDocumentTransformer(
            (documento, _, _) => EsquemaDeSegurancaOpenApi.AdicionarAsync(documento)));

        services.AddSqlServerPersistence(configuration);
        services.AddScoped<CadastroService>();
        services.AddScoped<CatalogoService>();
        services.AddScoped<OrdemServicoService>();
        services.AddScoped<GestaoOrdemServicoService>();
        services.AddScoped<DecisaoOrcamentoService>();
        services.AddScoped<ReservaEstoqueService>();
        services.AddScoped<ITentativaReservaEstoque>(provider => provider.GetRequiredService<ReservaEstoqueService>());
        services.AddScoped<ExecucaoOrdemServicoService>();
        services.AddScoped<AditivoService>();

        var jwt = ConfiguracaoJwt.Ler(configuration);
        services.AddSingleton(jwt);
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEmissorTokenInterno, EmissorJwt>();
        services.AddScoped<IIdentidadeInterna, IdentidadeInternaAdapter>();
        services.AddScoped<AcessoInternoService>();

        services.AddIdentityCore<UsuarioInterno>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentidadeDbContext>()
            .AddDefaultTokenProviders()
            .AddSignInManager();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Emissor,
                    ValidateAudience = true,
                    ValidAudience = jwt.Publico,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(jwt.Chave),
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = "unique_name",
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("EquipeInterna", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Todos))
            .AddPolicy("Cadastros", policy => policy.RequireAuthenticatedUser().RequireRole(
                PapeisInternos.Administrador, PapeisInternos.Atendente))
            .AddPolicy("AtendentesEMecanicos", policy => policy.RequireAuthenticatedUser().RequireRole(
                PapeisInternos.Atendente, PapeisInternos.Mecanico))
            .AddPolicy("Mecanicos", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Mecanico))
            .AddPolicy("Atendentes", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Atendente))
            .AddPolicy("Estoquistas", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Estoquista))
            .AddPolicy("Administradores", policy => policy.RequireAuthenticatedUser().RequireRole(PapeisInternos.Administrador));

        return services;
    }
}
